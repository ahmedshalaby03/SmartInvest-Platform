import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly usernameOrEmail = signal('');
  protected readonly password = signal('');
  protected readonly showPassword = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected imgFailed = false;

  protected submit(): void {
    if (this.loading()) {
      return;
    }
    this.error.set(null);

    if (!this.usernameOrEmail().trim() || !this.password()) {
      this.error.set('برجاء إدخال البريد/اسم المستخدم وكلمة المرور');
      return;
    }

    this.loading.set(true);
    this.auth
      .login({ usernameOrEmail: this.usernameOrEmail().trim(), password: this.password() })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.router.navigateByUrl(this.auth.homeRouteForRole(result.role));
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message ?? 'تعذّر تسجيل الدخول، تأكد من البيانات');
        },
      });
  }
}
