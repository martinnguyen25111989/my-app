import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  template: `
    <div class="auth-container">
      <mat-card class="auth-card">
        <mat-card-header>
          <mat-card-title>💰 Quản lý Thu Chi</mat-card-title>
          <mat-card-subtitle>Tạo tài khoản mới</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Họ và tên</mat-label>
              <input matInput formControlName="fullName" autocomplete="name">
              <mat-icon matSuffix>person</mat-icon>
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <input matInput formControlName="email" type="email" autocomplete="email">
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Mật khẩu (tối thiểu 6 ký tự)</mat-label>
              <input matInput formControlName="password" type="password" autocomplete="new-password">
              <mat-icon matSuffix>lock</mat-icon>
            </mat-form-field>
            @if (error()) {
              <p class="error-message">{{ error() }}</p>
            }
            <button mat-raised-button color="primary" class="full-width" type="submit"
                    [disabled]="form.invalid || loading()">
              @if (loading()) { <mat-spinner diameter="20" /> } @else { Đăng ký }
            </button>
          </form>
        </mat-card-content>
        <mat-card-actions>
          <a mat-button routerLink="/login">Đã có tài khoản? Đăng nhập</a>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .auth-container {
      display: flex; align-items: center; justify-content: center;
      min-height: 100vh; padding: 16px;
      background: linear-gradient(135deg, #1976d2 0%, #512da8 100%);
    }
    .auth-card { width: 100%; max-width: 400px; padding: 16px; }
    .error-message { color: #c62828; margin: 0 0 12px; font-size: 14px; }
    mat-card-header { margin-bottom: 16px; }
  `]
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    const { email, fullName, password } = this.form.getRawValue();
    this.auth.register(email, fullName, password).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Đăng ký thất bại. Vui lòng thử lại.');
      }
    });
  }
}
