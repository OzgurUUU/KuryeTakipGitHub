import { EventEmitter, Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

// Kurye ID'sini string yaptık çünkü backend'de "kurye_ahmet" gibi değerler dönüyor
export interface CourierLocation {
  id: string; 
  lat: number;
  lon: number;
  vehicleType?: string;
}

// Yeni mikroservisimizden gelecek olan "Sipariş Atandı" verisi için arayüz
export interface OrderAssignedPayload {
  orderId: string; // Guid olduğu için string
  driverId: string;
  distance: number;
  time: string; // C# tarafındaki DateTime, JSON ile string olarak gelir
}

export interface OrderCreatedPayload {
  orderId: string;
  customerName: string;
  latitude: number;
  longitude: number;
  itemDescription: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection!: signalR.HubConnection;

  public locationUpdates = new Subject<CourierLocation>();
  public courierDeleted = new Subject<string>(); 
  public orderDelivered = new EventEmitter<string>();
  
  // Yeni: Atama bildirimlerini componentlere iletmek için
  public orderAssigned = new Subject<OrderAssignedPayload>(); 
  
  // Yeni: Oto siparis bildirimlerini componentlere iletmek
  public orderCreated = new Subject<OrderCreatedPayload>();

  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      // 🚀 Gateway portu (5229) ve yeni hub rotamız (/logisticsHub)
      .withUrl('http://localhost:5229/logisticsHub')
      .withAutomaticReconnect() // Olası kopmalarda otomatik yeniden bağlanmayı sağlar
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('⚡ SignalR Gateway Üzerinden Başarıyla Bağlandı!'))
      .catch(err => console.error('❌ SignalR Bağlantı Hatası:', err));

    // 0. Yeni Sipariş Dinleyicisi
    this.hubConnection.on('OrderCreated', (payload: OrderCreatedPayload) => {
      console.log('📦 [SignalR] Yeni Sipariş Geldi:', payload);
      this.orderCreated.next(payload);
    });

    // 1. Yeni Eklediğimiz "Sipariş Atandı" Dinleyicisi
    this.hubConnection.on('OrderAssigned', (payload: OrderAssignedPayload) => {
      
      console.log('🎯 [SignalR] Yeni Kurye Ataması Yakalandı:', payload);
      this.orderAssigned.next(payload);
    });

    // 2. Canlı Konum Akışı 
    this.hubConnection.on('ReceiveLocation', (id: string, lat: number, lon: number, vehicleType: string) => {
      console.log(`📍 [SignalR] Konum Geldi: ${id} -> ${lat}, ${lon}`);
      this.locationUpdates.next({ id, lat, lon, vehicleType });
    });

    // 3. Silinen Kuryeler
    this.hubConnection.on('CourierDeleted', (id: string) => {
      this.courierDeleted.next(id);
    });

    // 4. Teslim Edilen Siparişler
    this.hubConnection.on('OrderDelivered', (orderId: string) => {
      this.orderDelivered.emit(orderId);
    });
  }
}