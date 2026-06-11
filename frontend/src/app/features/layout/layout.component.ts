import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable, map, shareReplay } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    AsyncPipe, RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatSidenavModule, MatListModule, MatIconModule,
    MatButtonModule, MatMenuModule, MatTooltipModule
  ],
  template: `
    <mat-sidenav-container class="layout-container">
      <mat-sidenav #drawer class="sidenav" fixedInViewport
                   [mode]="(isHandset$ | async) ? 'over' : 'side'"
                   [opened]="(isHandset$ | async) === false">
        <mat-toolbar class="app-toolbar">💰 Thu Chi</mat-toolbar>
        <mat-nav-list>
          @for (item of menu; track item.route) {
            <a mat-list-item [routerLink]="item.route" routerLinkActive="active-link"
               (click)="(isHandset$ | async) && drawer.close()">
              <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
              <span matListItemTitle>{{ item.label }}</span>
            </a>
          }
        </mat-nav-list>
      </mat-sidenav>
      <mat-sidenav-content>
        <mat-toolbar color="primary" class="app-toolbar">
          @if (isHandset$ | async) {
            <button mat-icon-button (click)="drawer.toggle()">
              <mat-icon>menu</mat-icon>
            </button>
          }
          <span>Quản lý Thu Chi</span>
          <span class="spacer"></span>
          <button mat-icon-button (click)="theme.toggle()"
                  [matTooltip]="theme.isDark() ? 'Chế độ sáng' : 'Chế độ tối'">
            <mat-icon>{{ theme.isDark() ? 'light_mode' : 'dark_mode' }}</mat-icon>
          </button>
          <button mat-icon-button [matMenuTriggerFor]="userMenu">
            <mat-icon>account_circle</mat-icon>
          </button>
          <mat-menu #userMenu="matMenu">
            <div class="user-info">
              <strong>{{ auth.currentUser()?.fullName }}</strong>
              <small>{{ auth.currentUser()?.email }}</small>
            </div>
            <button mat-menu-item (click)="auth.logout()">
              <mat-icon>logout</mat-icon> Đăng xuất
            </button>
          </mat-menu>
        </mat-toolbar>
        <router-outlet />
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .layout-container { height: 100vh; }
    .sidenav { width: 230px; }
    .active-link { background: rgba(25, 118, 210, 0.12); }
    .user-info { padding: 8px 16px; display: flex; flex-direction: column; }
    .user-info small { color: gray; }
  `]
})
export class LayoutComponent {
  auth = inject(AuthService);
  theme = inject(ThemeService);
  private breakpointObserver = inject(BreakpointObserver);

  menu = [
    { route: '/dashboard', icon: 'dashboard', label: 'Tổng quan' },
    { route: '/transactions', icon: 'receipt_long', label: 'Giao dịch' },
    { route: '/categories', icon: 'category', label: 'Danh mục' },
    { route: '/wallets', icon: 'account_balance_wallet', label: 'Ví tiền' },
    { route: '/budgets', icon: 'savings', label: 'Ngân sách' },
    { route: '/reports', icon: 'assessment', label: 'Báo cáo' },
    { route: '/import', icon: 'upload_file', label: 'Import sao kê' }
  ];

  isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset)
    .pipe(map(result => result.matches), shareReplay(1));
}
