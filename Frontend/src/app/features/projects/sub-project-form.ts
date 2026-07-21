import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ProjectsService } from '../../core/services/projects.service';
import { LookupsService } from '../../core/services/lookups.service';
import {
  Lookup,
  MainProjectListItem,
  MarkazLookup,
  SubProjectListItem,
} from '../../core/models/project.models';

export interface LockedParent {
  id: number;
  code: string;
  name: string;
}

@Component({
  selector: 'app-sub-project-form',
  imports: [FormsModule],
  template: `
    @if (open()) {
      <div class="si-overlay" (click)="close.emit()">
        <div class="si-modal" (click)="$event.stopPropagation()">
          <div class="si-modal-head">
            <div class="grow">
              <h3>{{ edit() ? 'تعديل مشروع فرعي' : 'إضافة مشروع فرعي' }}</h3>
              <p>{{ edit() ? 'الكود يُخصّص عند الاعتماد ولا يُعدّل من هنا' : 'يُضاف كمشروع مقترح بدون كود حتى اعتماد المدير' }}</p>
            </div>
            <button class="si-x" (click)="close.emit()" aria-label="إغلاق">×</button>
          </div>

          <div class="si-modal-body">
            @if (error()) { <div class="si-err">{{ error() }}</div> }

            <!-- المشروع الرئيسي -->
            @if (locked()) {
              <div class="si-locked">
                <div class="lh">
                  <div><b>{{ locked()!.name }}</b><div class="lc">الكود: {{ locked()!.code }}</div></div>
                  <span class="lb">🔒 المشروع الرئيسي التابع له</span>
                </div>
              </div>
            } @else {
              <div class="si-grid">
                <div class="si-fld full">
                  <label>المشروع الرئيسي <span class="req">*</span></label>
                  <select [ngModel]="mainProjectId()" (ngModelChange)="mainProjectId.set($event)" [disabled]="!!edit()">
                    <option [ngValue]="null">— اختر المشروع الرئيسي —</option>
                    @for (m of mains(); track m.id) { <option [ngValue]="m.id">{{ m.code }} — {{ m.name }}</option> }
                  </select>
                </div>
              </div>
            }

            <div class="si-note">
              <span>ℹ️</span>
              يُضاف المشروع الفرعي كـ «مقترح» بدون كود، ويحصل على كوده عند اعتماد مدير التخطيط.
            </div>

            <div class="si-step"><span class="n">2</span><h4>بيانات المشروع الفرعي</h4></div>
            <div class="si-grid">
              <div class="si-fld full">
                <label>اسم المشروع الفرعي <span class="req">*</span></label>
                <input [ngModel]="name()" (ngModelChange)="name.set($event)" placeholder="مثال: رصف طريق المحطة" />
              </div>
              <div class="si-fld">
                <label>المستوى <span class="req">*</span></label>
                <select [ngModel]="projectLevel()" (ngModelChange)="projectLevel.set($event)">
                  <option value="">— اختر —</option>
                  <option value="محلي">محلي</option>
                  <option value="مشترك">مشترك</option>
                </select>
              </div>
              <div class="si-fld">
                <label>المكوّن العيني</label>
                <select [ngModel]="componentType()" (ngModelChange)="componentType.set($event)">
                  <option value="">— اختر —</option>
                  @for (c of componentTypes; track c) { <option [value]="c">{{ c }}</option> }
                </select>
              </div>
              <div class="si-fld">
                <label>المركز <span class="req">*</span></label>
                <select [ngModel]="markazId()" (ngModelChange)="markazId.set($event)">
                  <option [ngValue]="null">— اختر —</option>
                  @for (mk of markazList(); track mk.id) { <option [ngValue]="mk.id">{{ mk.name }}</option> }
                </select>
              </div>
              <div class="si-fld">
                <label>الأولوية <span class="req">*</span></label>
                <select [ngModel]="priorityId()" (ngModelChange)="priorityId.set($event)">
                  <option [ngValue]="null">— اختر —</option>
                  @for (p of priorities(); track p.id) { <option [ngValue]="p.id">{{ p.name }}</option> }
                </select>
              </div>
              <div class="si-fld">
                <label>حالة المشروع <span class="req">*</span></label>
                <select [ngModel]="statusId()" (ngModelChange)="statusId.set($event)">
                  <option [ngValue]="null">— اختر —</option>
                  @for (st of statuses(); track st.id) { <option [ngValue]="st.id">{{ st.name }}</option> }
                </select>
              </div>
              <div class="si-fld">
                <label>تمويل بنكي (ج.م)</label>
                <input type="number" [ngModel]="bankFunding()" (ngModelChange)="bankFunding.set($event)" placeholder="0" />
              </div>
              <div class="si-fld">
                <label>تمويل ذاتي (ج.م)</label>
                <input type="number" [ngModel]="selfFunding()" (ngModelChange)="selfFunding.set($event)" placeholder="0" />
              </div>
              <div class="si-fld full">
                <label>ملاحظات / وصف</label>
                <textarea [ngModel]="description()" (ngModelChange)="description.set($event)" placeholder="أي ملاحظات إضافية…"></textarea>
              </div>
            </div>
          </div>

          <div class="si-modal-foot">
            <button class="si-btn primary" [disabled]="saving()" (click)="submit()">
              @if (saving()) { <span class="mini-sp"></span> جاري الحفظ… } @else { {{ edit() ? 'حفظ التعديلات' : 'إضافة المشروع الفرعي' }} }
            </button>
            <button class="si-btn" (click)="close.emit()">إلغاء</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`.mini-sp{width:14px;height:14px;border:2px solid rgba(255,255,255,.4);border-top-color:#fff;border-radius:50%;animation:spin .7s linear infinite;display:inline-block}@keyframes spin{to{transform:rotate(360deg)}}`],
})
export class SubProjectForm {
  private readonly projectsService = inject(ProjectsService);
  private readonly lookups = inject(LookupsService);

  readonly open = input(false);
  readonly edit = input<SubProjectListItem | null>(null);
  readonly locked = input<LockedParent | null>(null);
  readonly mains = input<MainProjectListItem[]>([]);
  readonly close = output<void>();
  readonly saved = output<void>();

  protected readonly priorities = signal<Lookup[]>([]);
  protected readonly statuses = signal<Lookup[]>([]);
  protected readonly markazList = signal<MarkazLookup[]>([]);

  protected readonly mainProjectId = signal<number | null>(null);
  protected readonly name = signal('');
  protected readonly projectLevel = signal('');
  protected readonly componentType = signal('');
  protected readonly markazId = signal<number | null>(null);
  protected readonly priorityId = signal<number | null>(null);
  protected readonly statusId = signal<number | null>(null);
  protected readonly bankFunding = signal<number>(0);
  protected readonly selfFunding = signal<number>(0);
  protected readonly description = signal('');

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  // قائمة ثابتة للمكوّن العيني (حقل نصي في الـ backend)
  protected readonly componentTypes = [
    'الات ومعدات',
    'الات ومعدات (نفقات)',
    'تجهيزات',
    'تشييدات',
    'شراء أرض',
    'مبانى سكنية',
    'مبانى غير سكنية',
  ];

  private lookupsLoaded = false;
  private wasOpen = false;

  constructor() {
    effect(() => {
      const isOpen = this.open();
      if (isOpen && !this.wasOpen) {
        this.wasOpen = true;
        this.onOpen();
      } else if (!isOpen) {
        this.wasOpen = false;
      }
    });
  }

  private onOpen(): void {
    this.error.set(null);
    this.ensureLookups(() => this.prefill());
  }

  private ensureLookups(done: () => void): void {
    if (this.lookupsLoaded) {
      done();
      return;
    }
    forkJoin({
      priorities: this.lookups.getPriorities(),
      statuses: this.lookups.getStatuses(),
      markaz: this.lookups.getMarkaz(),
    }).subscribe({
      next: ({ priorities, statuses, markaz }) => {
        this.priorities.set(priorities);
        this.statuses.set(statuses);
        this.markazList.set(markaz);
        this.lookupsLoaded = true;
        done();
      },
      error: () => this.error.set('تعذّر تحميل القوائم'),
    });
  }

  private prefill(): void {
    this.resetForm();
    const e = this.edit();
    const lockedParent = this.locked();

    if (lockedParent) {
      this.mainProjectId.set(lockedParent.id);
    }

    if (e) {
      // جلب التفاصيل الكاملة للتعديل
      this.projectsService.getSubProject(e.id).subscribe({
        next: (d) => {
          this.mainProjectId.set(d.mainProjectId);
          this.name.set(d.name);
          this.projectLevel.set(d.projectLevel);
          this.componentType.set(d.componentType);
          this.markazId.set(d.markazId);
          this.priorityId.set(d.priorityId);
          this.statusId.set(d.statusId);
          this.bankFunding.set(d.bankFunding);
          this.selfFunding.set(d.selfFunding);
          this.description.set(d.description ?? '');
        },
        error: () => this.error.set('تعذّر تحميل بيانات المشروع الفرعي'),
      });
    }
  }

  private resetForm(): void {
    this.mainProjectId.set(null);
    this.name.set('');
    this.projectLevel.set('');
    this.componentType.set('');
    this.markazId.set(null);
    this.priorityId.set(null);
    this.statusId.set(null);
    this.bankFunding.set(0);
    this.selfFunding.set(0);
    this.description.set('');
  }

  protected submit(): void {
    if (this.saving()) return;
    this.error.set(null);

    if (!this.name().trim()) { this.error.set('برجاء إدخال اسم المشروع الفرعي'); return; }
    if (!this.projectLevel()) { this.error.set('برجاء اختيار المستوى'); return; }
    if (this.markazId() == null) { this.error.set('برجاء اختيار المركز'); return; }
    if (this.priorityId() == null) { this.error.set('برجاء اختيار الأولوية'); return; }
    if (this.statusId() == null) { this.error.set('برجاء اختيار حالة المشروع'); return; }
    if (!this.edit() && this.mainProjectId() == null) { this.error.set('برجاء اختيار المشروع الرئيسي'); return; }

    const base = {
      name: this.name().trim(),
      projectLevel: this.projectLevel(),
      componentType: this.componentType().trim(),
      accountingUnit: '',
      projectNature: '',
      markazId: this.markazId()!,
      priorityId: this.priorityId()!,
      statusId: this.statusId()!,
      bankFunding: Number(this.bankFunding()) || 0,
      selfFunding: Number(this.selfFunding()) || 0,
      latitude: null,
      longitude: null,
      description: this.description().trim() || null,
    };

    this.saving.set(true);
    const editing = this.edit();
    const req = editing
      ? this.projectsService.updateSubProject(editing.id, base)
      : this.projectsService.createSubProject({ ...base, mainProjectId: this.mainProjectId()! });

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.emit();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'تعذّر حفظ المشروع الفرعي');
      },
    });
  }
}
