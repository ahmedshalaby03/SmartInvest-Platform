import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResult,
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  ResetPasswordRequest,
  Roles,
  UpdateProfileRequest,
  UserProfile,
} from '../models/auth.models';

const TOKEN_KEY = 'smartinvest_token';
const USER_KEY = 'smartinvest_user';
const AVATAR_CACHE_PREFIX = 'smartinvest_avatar_';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _user = signal<CurrentUser | null>(this.loadUser());
  private readonly _avatarUrl = signal<string | null>(this.loadCachedAvatar(this._user()));
  private avatarRequestId = 0;
  private avatarRetryCount = 0;
  private avatarRetryTimer: ReturnType<typeof setTimeout> | null = null;

  readonly user = this._user.asReadonly();
  readonly avatarUrl = this._avatarUrl.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly role = computed(() => this._user()?.role ?? null);
  readonly isSuperAdmin = computed(() => this._user()?.role === Roles.SuperAdmin);
  readonly isPlanningManager = computed(() => this._user()?.role === Roles.PlanningManager);
  readonly isFinancialManager = computed(() => this._user()?.role === Roles.FinancialManager);
  readonly isManager = computed(
    () => this._user()?.role === Roles.PlanningManager || this.isSuperAdmin(),
  );
  readonly isFinancial = computed(
    () =>
      this._user()?.role === Roles.FinancialManager ||
      this._user()?.role === Roles.FinancialEmployee,
  );
  readonly canEditProjects = computed(() => {
    const role = this._user()?.role;
    return role === Roles.PlanningEmployee || role === Roles.PlanningManager || role === Roles.SuperAdmin;
  });
  readonly canManageProjects = computed(() => this.isPlanningManager() || this.isSuperAdmin());
  readonly canEditFinancial = computed(() => {
    const role = this._user()?.role;
    return role === Roles.FinancialEmployee || role === Roles.FinancialManager || role === Roles.SuperAdmin;
  });
  readonly canManageFinancial = computed(() => this.isFinancialManager() || this.isSuperAdmin());
  // تسجيل إتاحة بنكية جديدة متاح لمدير التخطيط أيضًا، بينما يظل تعديلها وحذفها للإدارة المالية.
  readonly canAddBankAvailability = computed(() => this.canEditFinancial() || this.isPlanningManager());
  readonly canManageProcurementDuration = computed(
    () => this.isPlanningManager() || this.isSuperAdmin(),
  );
  readonly canManageContractors = computed(() => this.isFinancialManager() || this.isSuperAdmin());
  readonly canManageUsers = computed(() =>
    this.isPlanningManager() || this.isFinancialManager() || this.isSuperAdmin(),
  );

  constructor() {
    if (this._user()?.hasAvatar) {
      this.refreshAvatar();
    }
  }

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((result) => this.setSession(result)),
    );
  }

  logout(): void {
    this.clearAvatarUrl();
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  /** المسار الافتراضي بعد تسجيل الدخول حسب الدور */
  homeRouteForRole(role: string | null): string {
    return '/app/dashboard';
  }

  uploadAvatar(file: File): Observable<void> {
    const form = new FormData();
    form.append('file', file);
    return this.http.put<void>(`${environment.apiUrl}/auth/me/avatar`, form).pipe(
      tap(() => {
        const current = this._user();
        if (current) {
          const updated = { ...current, hasAvatar: true };
          this._user.set(updated);
          localStorage.setItem(USER_KEY, JSON.stringify(updated));
        }
        this.refreshAvatar();
      }),
    );
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${environment.apiUrl}/auth/me`).pipe(
      tap((profile) => this.updateCachedUser(profile)),
    );
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${environment.apiUrl}/auth/me`, request).pipe(
      tap((profile) => this.updateCachedUser(profile)),
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/change-password`, request);
  }

  deleteAvatar(): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/auth/me/avatar`).pipe(
      tap(() => {
        const current = this._user();
        if (current) {
          const updated = { ...current, hasAvatar: false };
          this._user.set(updated);
          localStorage.setItem(USER_KEY, JSON.stringify(updated));
        }
        this.clearAvatarUrl();
      }),
    );
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/forgot-password`, { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/reset-password`, request);
  }

  refreshAvatar(): void {
    const currentUser = this._user();
    const userId = currentUser?.userId;
    if (!userId) {
      return;
    }

    const requestId = ++this.avatarRequestId;
    this.http
      .get(`${environment.apiUrl}/auth/users/${userId}/avatar?v=${Date.now()}`, {
        responseType: 'blob',
      })
      .subscribe({
        next: (blob) => {
          this.blobToDataUrl(blob)
            .then((dataUrl) => {
              if (requestId !== this.avatarRequestId || this._user()?.userId !== userId) {
                return;
              }

              this.avatarRetryCount = 0;
              this.clearAvatarRetry();
              this._avatarUrl.set(dataUrl);
              this.cacheAvatar(userId, dataUrl);
            })
            .catch(() => this.handleAvatarLoadFailure(requestId, userId));
        },
        error: (error) => {
          if (requestId !== this.avatarRequestId) {
            return;
          }

          if (error?.status === 404) {
            this.markAvatarMissing();
            return;
          }

          this.handleAvatarLoadFailure(requestId, userId);
        },
      });
  }

  private clearAvatarUrl(): void {
    this.avatarRequestId++;
    this.avatarRetryCount = 0;
    this.clearAvatarRetry();

    const existing = this._avatarUrl();
    if (existing?.startsWith('blob:')) {
      URL.revokeObjectURL(existing);
    }
    const userId = this._user()?.userId;
    if (userId) {
      try {
        sessionStorage.removeItem(`${AVATAR_CACHE_PREFIX}${userId}`);
      } catch {
        // Ignore browsers where session storage is unavailable.
      }
    }
    this._avatarUrl.set(null);
  }

  private setSession(result: AuthResult): void {
    this.clearAvatarUrl();
    const user: CurrentUser = {
      userId: result.userId,
      fullName: result.fullName,
      email: result.email,
      role: result.role,
      hasAvatar: result.hasAvatar,
    };
    localStorage.setItem(TOKEN_KEY, result.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._user.set(user);
    if (user.hasAvatar) {
      this.refreshAvatar();
    } else {
      this.clearAvatarUrl();
    }
  }

  private updateCachedUser(profile: UserProfile): void {
    const current = this._user();
    if (!current) {
      return;
    }

    const updated: CurrentUser = {
      ...current,
      fullName: profile.fullName,
      email: profile.email,
      hasAvatar: profile.hasAvatar,
    };
    this._user.set(updated);
    localStorage.setItem(USER_KEY, JSON.stringify(updated));

    if (profile.hasAvatar && !this._avatarUrl()) {
      this.refreshAvatar();
    }
    if (!profile.hasAvatar) {
      this.clearAvatarUrl();
    }
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

  private loadCachedAvatar(user: CurrentUser | null): string | null {
    if (!user?.hasAvatar) {
      return null;
    }

    try {
      return sessionStorage.getItem(`${AVATAR_CACHE_PREFIX}${user.userId}`);
    } catch {
      return null;
    }
  }

  private cacheAvatar(userId: string, dataUrl: string): void {
    try {
      sessionStorage.setItem(`${AVATAR_CACHE_PREFIX}${userId}`, dataUrl);
    } catch {
      // A large image can exceed storage quota; the in-memory image still remains valid.
    }
  }

  private blobToDataUrl(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () =>
        typeof reader.result === 'string' ? resolve(reader.result) : reject(new Error('Invalid avatar'));
      reader.onerror = () => reject(reader.error ?? new Error('Unable to read avatar'));
      reader.readAsDataURL(blob);
    });
  }

  private handleAvatarLoadFailure(requestId: number, userId: string): void {
    if (
      requestId !== this.avatarRequestId ||
      this._user()?.userId !== userId ||
      this._avatarUrl() ||
      this.avatarRetryCount >= 2
    ) {
      return;
    }

    const delay = 600 * 2 ** this.avatarRetryCount;
    this.avatarRetryCount++;
    this.clearAvatarRetry();
    this.avatarRetryTimer = setTimeout(() => this.refreshAvatar(), delay);
  }

  private clearAvatarRetry(): void {
    if (this.avatarRetryTimer) {
      clearTimeout(this.avatarRetryTimer);
      this.avatarRetryTimer = null;
    }
  }

  private markAvatarMissing(): void {
    const current = this._user();
    if (current) {
      const updated = { ...current, hasAvatar: false };
      this._user.set(updated);
      localStorage.setItem(USER_KEY, JSON.stringify(updated));
    }
    this.clearAvatarUrl();
  }
}
