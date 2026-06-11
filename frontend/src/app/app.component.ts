import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme.service';
import { ReminderService } from './core/reminder.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class AppComponent {
  // Khởi tạo theme (dark mode) và nhắc nhở nhập chi tiêu hằng ngày
  private theme = inject(ThemeService);
  private reminder = inject(ReminderService);
}
