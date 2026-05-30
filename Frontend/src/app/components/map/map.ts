import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, EventEmitter, NgZone, OnInit, Output } from '@angular/core';
import * as L from 'leaflet';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule],
  template: `<div id="map" class="w-full h-full z-0"></div>`,
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    /* Opsiyonel: Atama çizgisi için kesik çizgi animasyonu */
    ::ng-deep .assignment-line {
      stroke-dasharray: 10, 10;
      animation: dash 20s linear infinite;
    }
    @keyframes dash {
      to { stroke-dashoffset: -1000; }
    }
  `]
})
export class Map implements OnInit, AfterViewInit {
  private map!: L.Map;
  
  // 🚀 TÜM DİZİ KEY'LERİ STRING OLARAK GÜNCELLENDİ
  private markers: { [id: string]: L.Marker } = {};
  private history: { [id: string]: L.LatLngExpression[] } = {};
  private polylines: { [id: string]: L.Polyline } = {};
  private orderMarkers: { [id: string]: L.Marker } = {};
  private assignmentLines: { [orderId: string]: L.Polyline } = {}; // Yeni: Atama çizgileri için

  private routeColors = ['#ef4444', '#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899'];

  @Output() locationDropped = new EventEmitter<any>();

  constructor(private zone: NgZone) {}

  ngOnInit() {}

  ngAfterViewInit() {
    this.initMap();
  }

  private initMap(): void {
    this.map = L.map('map', {
      center: [37.0000, 35.3200], // Adana
      zoom: 14,
      zoomControl: false
    });

    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
      attribution: '© OpenStreetMap contributors, © CARTO'
    }).addTo(this.map);

    L.control.zoom({ position: 'bottomright' }).addTo(this.map);

    const mapContainer = document.getElementById('map');
    mapContainer?.addEventListener('dragover', (e) => e.preventDefault());
    mapContainer?.addEventListener('drop', (e: DragEvent) => {
      e.preventDefault();
      const point = this.map.mouseEventToLatLng(e);
      const dragType = e.dataTransfer?.getData('drag-type') || 'courier';
      
      this.zone.run(() => this.locationDropped.emit({ lat: point.lat, lng: point.lng, type: dragType } as any));
    });
  }

  // String ID'den rastgele ama tutarlı bir renk üreten yardımcı metod
  private getColorForId(id: string): string {
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
      hash = id.charCodeAt(i) + ((hash << 5) - hash);
    }
    return this.routeColors[Math.abs(hash) % this.routeColors.length];
  }

  private getIconForVehicle(vehicleType: string): L.DivIcon {
    let emoji = '📦';
    if (vehicleType === 'Motorcycle') emoji = '🏍️';
    else if (vehicleType === 'Bicycle') emoji = '🚲';
    else if (vehicleType === 'Car') emoji = '🚗';
    else if (vehicleType === 'Scooter') emoji = '🛴';

    return L.divIcon({
      html: `<div style="font-size: 28px; text-shadow: 0px 0px 8px rgba(255,255,255,0.4);">${emoji}</div>`,
      className: 'custom-vehicle-icon',
      iconSize: [30, 30],
      iconAnchor: [15, 15]
    });
  }

  // 🚀 ID PARAMETRELERİ STRING OLDU
  public updateMarker(id: string, lat: number, lon: number, vehicleType: string) {
    const newPoint: L.LatLngExpression = [lat, lon];
    
    if (!this.history[id]) this.history[id] = [];
    this.history[id].push(newPoint);
    if (this.history[id].length > 15) this.history[id].shift();

    if (this.polylines[id]) {
      this.polylines[id].setLatLngs(this.history[id]);
    } else {
      const color = this.getColorForId(id); // Yeni renk algoritması
      this.polylines[id] = L.polyline(this.history[id], { color, weight: 4, opacity: 0.7, dashArray: '5, 10' }).addTo(this.map);
    }

    if (this.markers[id]) {
      this.markers[id].setLatLng(newPoint);
    } else {
      const icon = this.getIconForVehicle(vehicleType);
      this.markers[id] = L.marker(newPoint, { icon }).addTo(this.map);
      this.markers[id].bindPopup(`
        <div class="text-center">
          <b>Kurye:</b> ${id} <br> 
          <span class="text-gray-500 text-xs">${vehicleType}</span>
        </div>
      `).openPopup();
    }
  }

  public removeCourier(id: string) {
    if (this.markers[id]) { this.map.removeLayer(this.markers[id]); delete this.markers[id]; }
    if (this.polylines[id]) { this.map.removeLayer(this.polylines[id]); delete this.polylines[id]; }
    delete this.history[id];
  }

  public flyToLocation(lat: number, lon: number, id: string) {
    this.map.flyTo([lat, lon], 16, { animate: true, duration: 1.5 });
    this.markers[id]?.openPopup();
  }

  public addOrderMarker(orderId: string, lat: number, lon: number) {
    const orderIcon = L.divIcon({
      html: `<div style="font-size: 26px; text-shadow: 0px 0px 10px rgba(249,115,22,0.9);">📦</div>`,
      className: 'custom-order-icon',
      iconSize: [30, 30],
      iconAnchor: [15, 15]
    });

    const marker = L.marker([lat, lon], { icon: orderIcon }).addTo(this.map);
    marker.bindPopup(`
      <div class="text-center">
        <b class="text-orange-500">Sipariş #${orderId.substring(0,8)}</b><br>
        <span class="text-xs text-gray-500">Kurye Yolda...</span>
      </div>
    `).openPopup();

    this.orderMarkers[orderId] = marker;
  }

  public removeOrderMarker(orderId: string) {
    if (this.orderMarkers[orderId]) {
      this.map.removeLayer(this.orderMarkers[orderId]);
      delete this.orderMarkers[orderId];
    }
    // Varsa atama çizgisini de temizle
    if (this.assignmentLines[orderId]) {
      this.map.removeLayer(this.assignmentLines[orderId]);
      delete this.assignmentLines[orderId];
    }
  }

  // 🚀 YENİ METOD: RabbitMQ'dan atama geldiğinde kurye ile siparişi birbirine bağlar
  public drawLineBetweenOrderAndCourier(orderId: string, courierId: string) {
    const courierMarker = this.markers[courierId];
    const orderMarker = this.orderMarkers[orderId];

    if (courierMarker && orderMarker) {
      const latlngs = [courierMarker.getLatLng(), orderMarker.getLatLng()];
      
      // Eğer daha önce çizilmişse temizle
      if (this.assignmentLines[orderId]) {
         this.map.removeLayer(this.assignmentLines[orderId]);
      }
      
      // Turuncu, kesik ve css ile hareketli atama çizgisi çek
      const line = L.polyline(latlngs, { color: '#f97316', weight: 3, className: 'assignment-line' }).addTo(this.map);
      this.assignmentLines[orderId] = line;
      
      // Sipariş popup'ını kurye atandı şeklinde güncelle
      orderMarker.setPopupContent(`
        <div class="text-center">
          <b class="text-orange-500">Sipariş #${orderId.substring(0,8)}</b><br>
          <span class="text-xs font-bold text-green-500">Kurye: ${courierId} atandı!</span>
        </div>
      `).openPopup();
    }
  }
}