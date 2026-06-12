import { Injectable } from '@angular/core';

const REMINDER_KEY = 'em_last_reminder';
const REMINDER_HOUR = 20; // nhắc lúc 20h hằng ngày

/**
 * Nhắc nhập chi tiêu hằng ngày bằng Web Notification API.
 * Kiểm tra mỗi phút khi app đang mở; nếu đã qua giờ nhắc và hôm nay chưa nhắc thì hiển thị thông báo.
 */
@Injectable({ providedIn: 'root' })
export class ReminderService {
  constructor() {
    this.requestPermission();
    setInterval(() => this.checkAndNotify(), 60_000);
    this.checkAndNotify();
  }

  private requestPermission(): void {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  }

  private checkAndNotify(): void {
    if (!('Notification' in window) || Notification.permission !== 'granted') return;

    const now = new Date();
    const today = now.toISOString().slice(0, 10);
    const lastReminded = localStorage.getItem(REMINDER_KEY);

    if (now.getHours() >= REMINDER_HOUR && lastReminded !== today) {
      new Notification('Quản lý Thu Chi 💰', {
        body: 'Đừng quên ghi lại các khoản chi tiêu hôm nay nhé!',
        icon: 'icons/icon-192.png'
      });
      localStorage.setItem(REMINDER_KEY, today);
    }
  }
}
