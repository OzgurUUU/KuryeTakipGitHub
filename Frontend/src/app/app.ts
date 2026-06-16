import { Component, NgZone, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CourierLocation, OrderAssignedPayload, SignalrService } from './services/signalr-service';

import { Sidebar } from './components/sidebar/sidebar';
import { Map } from './components/map/map';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: true,
  styleUrls: ['./app.scss'],
  imports: [CommonModule, FormsModule, Sidebar, Map]
})
export class App implements OnInit {

  @ViewChild(Map) mapComponent!: Map;

  // VERİ DURUMU (STATE) - ID'ler string oldu ("kurye_ahmet", "edcb5d90..." vb.)
  public couriersList: { id: string, lat: number, lon: number }[] = [];
  public activeCouriersCount = 0;
  public recentLogs: string[] = [];

  // YENİ KURYE FORMU DURUMU
  public isModalOpen = false;
  public newCourierData = { name: '', vehicleType: 'Motorcycle', lastLatitude: 0, lastLongitude: 0 };
  
  // YENİ SİPARİŞ FORMU DURUMU - Backend'in beklediği modele göre güncellendi
  public isOrderModalOpen = false;
  public newOrderData = { customerName: '', latitude: 0, longitude: 0, itemDescription: 'Standart Paket' };

  constructor(private signalrService: SignalrService, private zone: NgZone) { }

  ngOnInit() {
    this.signalrService.startConnection();

    // 0. Backend'den oto siparis geldiginde (Simulator):
    this.signalrService.orderCreated.subscribe((payload: any) => {
      this.mapComponent.addOrderMarker(payload.orderId, payload.latitude, payload.longitude);
      this.recentLogs.unshift(`📱 [Oto Sipariş] ${payload.customerName} - ${payload.itemDescription}`);
      if (this.recentLogs.length > 5) this.recentLogs.pop();
    });

    // 1. Backend'den kurye konumu geldiğinde:
    this.signalrService.locationUpdates.subscribe((data: CourierLocation) => {
      this.mapComponent.updateMarker(data.id, data.lat, data.lon, data.vehicleType || 'Motorcycle');
      this.updateLocalList(data);
    });

    // 2. 🚀 YENİ MİMARİ: Sipariş Ataması Geldiğinde (RabbitMQ -> SignalR)
    this.signalrService.orderAssigned.subscribe((payload: OrderAssignedPayload) => {
      this.recentLogs.unshift(`🎯 ATAMA: Sipariş #${payload.orderId.substring(0,8)} -> Kurye: ${payload.driverId} (${payload.distance.toFixed(2)} km)`);
      if (this.recentLogs.length > 5) this.recentLogs.pop();
      
      // 🚀 İŞTE BURASI: Haritada atama çizgisini çekiyoruz!
      this.mapComponent.drawLineBetweenOrderAndCourier(payload.orderId, payload.driverId);
    });

    // 3. Backend'den silinme emri geldiğinde:
    this.signalrService.courierDeleted.subscribe((id: string) => {
      this.mapComponent.removeCourier(id);
      this.removeFromLocalList(id);
    });

    // 4. Teslimat gerçekleştiğinde:
    this.signalrService.orderDelivered.subscribe((orderId: string) => {
      this.mapComponent.removeOrderMarker(orderId);
      this.recentLogs.unshift(`✅ Sipariş #${orderId.substring(0,8)} teslim edildi!`);
    });
  }

  // --- STATE YÖNETİM METODLARI ---

  private updateLocalList(data: CourierLocation): void {
    const existingCourier = this.couriersList.find(c => c.id === data.id);
    if (existingCourier) {
      existingCourier.lat = data.lat;
      existingCourier.lon = data.lon;
    } else {
      this.couriersList.push({ id: data.id, lat: data.lat, lon: data.lon });
      this.activeCouriersCount++;
    }

    this.recentLogs.unshift(`Kurye ${data.id} hareket etti: ${data.lat.toFixed(4)}, ${data.lon.toFixed(4)}`);
    if (this.recentLogs.length > 5) this.recentLogs.pop();
  }

  private removeFromLocalList(id: string): void {
    this.couriersList = this.couriersList.filter(c => c.id !== id);
    this.activeCouriersCount--;

    this.recentLogs.unshift(`🚨 Kurye ${id} sistemden çıkarıldı.`);
    if (this.recentLogs.length > 5) this.recentLogs.pop();
  }

  // --- ÇOCUKLARDAN (CHILD) GELEN EVENTLER ---

  public focusOnCourier(id: string): void {
    const courier = this.couriersList.find(c => c.id === id);
    if (courier) {
      this.mapComponent.flyToLocation(courier.lat, courier.lon, id);
    }
  }

  public onLocationDropped(data: { lat: number, lng: number, type: string }): void {
    if (data.type === 'order') {
      this.newOrderData.latitude = data.lat;
      this.newOrderData.longitude = data.lng;
      this.isOrderModalOpen = true;
    } else {
      this.newCourierData.lastLatitude = data.lat;
      this.newCourierData.lastLongitude = data.lng;
      this.isModalOpen = true;
    }
  }

  // --- API İSTEKLERİ ---

  async saveOrder() {
    try {
      // 🚀 YARP Gateway Portu ve Yeni Rota Kullanılıyor
      const response = await fetch('http://localhost:5229/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(this.newOrderData)
      });

      if (response.ok) {
        const result = await response.json();

        // Sadece siparişi haritaya ekliyoruz. Atama SignalR'dan gelecek!
        this.mapComponent.addOrderMarker(result.orderId, this.newOrderData.latitude, this.newOrderData.longitude);

        this.recentLogs.unshift(`📦 Sipariş alındı, sistem en yakın kuryeyi arıyor...`);
        if (this.recentLogs.length > 5) this.recentLogs.pop();

        this.isOrderModalOpen = false;
        this.newOrderData = { customerName: '', latitude: 0, longitude: 0, itemDescription: 'Standart Paket' };

      } else {
        alert("Sipariş alınamadı. API tarafında bir hata oluştu.");
        this.isOrderModalOpen = false;
      }
    } catch (error) {
      console.error("Sipariş verilirken hata oluştu:", error);
      alert("Gateway bağlantı hatası!");
    }
  }

  async saveCourier() {
    try {
      // 🚀 YARP Gateway üzerinden kurye takip servisine yönlendirme
      const response = await fetch('http://localhost:5229/api/tracking/add', { 
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(this.newCourierData)
      });

      if (response.ok) {
        this.isModalOpen = false;
        this.newCourierData = { name: '', vehicleType: 'Motorcycle', lastLatitude: 0, lastLongitude: 0 };
      } else {
        alert("Sunucu reddetti! Backend API uç noktasını kontrol et.");
      }
    } catch (error) {
      console.error("Kurye eklenirken hata oluştu:", error);
      alert("Gateway bağlantı hatası!");
    }
  }
}