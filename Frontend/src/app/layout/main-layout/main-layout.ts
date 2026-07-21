import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { Roles } from '../../core/models/auth.models';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  managerOnly: boolean;
}

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.user;
  protected readonly isManager = this.auth.isManager;

  protected readonly roleLabel = computed(() =>
    this.auth.role() === Roles.PlanningManager ? 'مدير التخطيط' : 'موظف تخطيط',
  );

  protected readonly initial = computed(() => this.user()?.fullName?.trim()?.charAt(0) ?? '؟');

  protected readonly today = new Date().toLocaleDateString('ar-EG', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  private readonly allNav: NavItem[] = [
    { label: 'لوحة التحكم', route: '/app/dashboard', icon: 'M4 13h6V4H4v9Zm10 7h6v-9h-6v9ZM4 20h6v-4H4v4ZM14 4v5h6V4h-6Z', managerOnly: true },
    { label: 'المشروعات', route: '/app/projects', icon: 'M3 7h18M3 12h18M3 17h18', managerOnly: false },
    { label: 'إدارة المستخدمين', route: '/app/users', icon: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', managerOnly: true },
  ];

  protected readonly nav = computed(() =>
    this.allNav.filter((item) => !item.managerOnly || this.isManager()),
  );

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
