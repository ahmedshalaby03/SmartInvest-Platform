import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UsersService } from '../../core/services/users.service';
import { AppUser } from '../../core/models/user.models';
import { Roles } from '../../core/models/auth.models';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-users',
  imports: [FormsModule, DatePipe],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class Users {
  private readonly usersService = inject(UsersService);
  protected readonly Roles = Roles;

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly users = signal<AppUser[]>([]);
  protected readonly search = signal('');
  protected readonly statusFilter = signal<StatusFilter>('all');

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const sf = this.statusFilter();
    return this.users().filter((u) => {
      const matchTerm =
        !term ||
        u.fullName.toLowerCase().includes(term) ||
        u.email.toLowerCase().includes(term) ||
        u.userName.toLowerCase().includes(term);
      const matchStatus = sf === 'all' || (sf === 'active' ? u.isActive : !u.isActive);
      return matchTerm && matchStatus;
    });
  });

  protected readonly total = computed(() => this.users().length);
  protected readonly activeCount = computed(() => this.users().filter((u) => u.isActive).length);
  protected readonly inactiveCount = computed(() => this.users().filter((u) => !u.isActive).length);
  protected readonly managerCount = computed(
    () => this.users().filter((u) => u.role === Roles.PlanningManager).length,
  );

  // ===== pagination =====
  protected readonly page = signal(1);
  protected readonly pageSize = 8;
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  protected readonly paged = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });
  protected readonly rangeStart = computed(() =>
    this.filtered().length === 0 ? 0 : (this.page() - 1) * this.pageSize + 1,
  );
  protected readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize, this.filtered().length),
  );

  protected goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages()) {
      this.page.set(p);
    }
  }

  // ===== add-user modal =====
  protected readonly showForm = signal(false);
  protected readonly fFullName = signal('');
  protected readonly fUserName = signal('');
  protected readonly fEmail = signal('');
  protected readonly fPhone = signal('');
  protected readonly fPassword = signal('');
  protected readonly fConfirm = signal('');
  protected readonly fRole = signal<string>(Roles.PlanningEmployee);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  constructor() {
    this.load();
    effect(() => {
      this.search();
      this.statusFilter();
      this.page.set(1);
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.usersService.getUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل المستخدمين. تأكد من تسجيل الدخول كمدير تخطيط.');
        this.loading.set(false);
      },
    });
  }

  protected initial(name: string): string {
    return name?.trim()?.charAt(0) ?? '؟';
  }

  protected roleLabel(role: string): string {
    return role === Roles.PlanningManager ? 'مدير التخطيط' : 'موظف تخطيط';
  }

  protected toggleActive(u: AppUser): void {
    const req = u.isActive ? this.usersService.deactivate(u.id) : this.usersService.activate(u.id);
    req.subscribe({
      next: () => this.load(),
      error: (err) => alert(err?.error?.message ?? 'تعذّر تغيير حالة المستخدم'),
    });
  }

  protected resetPassword(u: AppUser): void {
    const pwd = prompt(`كلمة المرور الجديدة للمستخدم «${u.fullName}»:`);
    if (!pwd) {
      return;
    }
    this.usersService.resetPassword(u.id, pwd).subscribe({
      next: () => alert('تم إعادة تعيين كلمة المرور بنجاح'),
      error: (err) => alert(err?.error?.message ?? 'تعذّر إعادة تعيين كلمة المرور'),
    });
  }

  // ===== add form =====
  protected openForm(): void {
    this.fFullName.set('');
    this.fUserName.set('');
    this.fEmail.set('');
    this.fPhone.set('');
    this.fPassword.set('');
    this.fConfirm.set('');
    this.fRole.set(Roles.PlanningEmployee);
    this.formError.set(null);
    this.showForm.set(true);
  }

  protected closeForm(): void {
    this.showForm.set(false);
  }

  protected submitForm(): void {
    if (this.saving()) return;
    this.formError.set(null);

    if (!this.fFullName().trim() || !this.fUserName().trim() || !this.fEmail().trim()) {
      this.formError.set('الاسم الكامل واسم المستخدم والبريد الإلكتروني مطلوبين');
      return;
    }
    if (!this.fPassword()) {
      this.formError.set('برجاء إدخال كلمة المرور');
      return;
    }
    if (this.fPassword() !== this.fConfirm()) {
      this.formError.set('كلمة المرور وتأكيدها غير متطابقين');
      return;
    }

    this.saving.set(true);
    this.usersService
      .createEmployee({
        fullName: this.fFullName().trim(),
        userName: this.fUserName().trim(),
        email: this.fEmail().trim(),
        phoneNumber: this.fPhone().trim() || null,
        password: this.fPassword(),
        role: this.fRole(),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.showForm.set(false);
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          this.formError.set(err?.error?.message ?? 'تعذّر إنشاء المستخدم');
        },
      });
  }
}
