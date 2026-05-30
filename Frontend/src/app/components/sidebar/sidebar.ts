import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  standalone: true
})
export class Sidebar {
  @Input() activeCouriersCount: number = 0;
  
  // 🚀 ID tipi string olarak güncellendi
  @Input() couriersList: { id: string, lat: number, lon: number }[] = [];
  @Input() recentLogs: string[] = [];

  // 🚀 Emit edilecek tip string olarak güncellendi
  @Output() courierFocused = new EventEmitter<string>();

  onDragStart(event: DragEvent, type: string) {
    event.dataTransfer?.setData('drag-type', type);
  }

  // 🚀 Parametre tipi string olarak güncellendi
  triggerFocus(id: string) {
    this.courierFocused.emit(id);
  }
}