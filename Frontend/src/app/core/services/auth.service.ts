import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, CurrentUser, LoginRequest, Roles } from '../models/auth.models';

const TOKEN_KEY = 'smartinvest_token';
const USER_KEY = 'smartinvest_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _user = signal<CurrentUser | null>(this.loadUser());

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly role = computed(() => this._user()?.role ?? null);
  readonly isManager = computed(() => this._user()?.role === Roles.PlanningManager);

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((result) => this.setSession(result)),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  /** المسار الافتراضي بعد تسجيل الدخول حسب الدور */
  homeRouteForRole(role: string | null): string {
    if (role === Roles.PlanningManager) {
      return '/app/dashboard';
    }
    return '/app/projects';
  }

  private setSession(result: AuthResult): void {
    const user: CurrentUser = {
      userId: result.userId,
      fullName: result.fullName,
      email: result.email,
      role: result.role,
    };
    localStorage.setItem(TOKEN_KEY, result.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._user.set(user);
  }

  private loadUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
