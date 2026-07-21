import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ProjectsService } from '../../core/services/projects.service';
import { LookupsService } from '../../core/services/lookups.service';
import { EXECUTING_AGENCIES, Lookup, MainProjectListItem, SubProgramLookup } from '../../core/models/project.models';

@Component({
  selector: 'app-main-project-form',
  imports: [FormsModule],
  template: `
    @if (open()) {
      <div class="si-overlay" (click)="close.emit()">
        <div class="si-modal" (click)="$event.stopPropagation()">
          <div class="si-modal-head">
            <div class="grow">
              <h3>{{ edit() ? 'تعديل مشروع رئيسي' : 'إضافة مشروع رئيسي' }}</h3>
              <p>{{ edit() ? 'الكود ثابت ولا يمكن تغييره بعد الإنشاء' : 'يُنشأ المشروع الرئيسي أولًا ثم تُضاف تحته المشاريع الفرعية' }}</p>
            </div>
            <button class="si-x" (click)="close.emit()" aria-label="إغلاق">×</button>
          </div>

          <div class="si-modal-body">
            @if (error()) { <div class="si-err">{{ error() }}</div> }

            <div class="si-step"><span class="n">1</span><h4>بيانات المشروع الرئيسي</h4></div>
            <div class="si-grid">
              <div class="si-fld">
                <label>البرنامج الرئيسي <span class="req">*</span></label>
                <select [ngModel]="programId()" (ngModelChange)="onProgramChange($event)">
                  <option [ngValue]="null">— اختر —</option>
                  @for (p of programs(); track p.id) { <option [ngValue]="p.id">{{ p.name }}</option> }
                </select>
              </div>
              <div class="si-fld">
                <label>البرنامج الفرعي <span class="req">*</span></label>
                <select [ngModel]="subProgramId()" (ngModelChange)="subProgramId.set($event)">
                  <option [ngValue]="null">— اختر —</option>
                  @for (s of filteredSubPrograms(); track s.id) { <option [ngValue]="s.id">{{ s.name }}</option> }
                </select>
              </div>
              <div class="si-fld full">
                <label>اسم المشروع الرئيسي <span class="req">*</span></label>
                <input [ngModel]="name()" (ngModelChange)="name.set($event)" placeholder="مثال: تطوير شبكة الطرق الداخلية بشبين الكوم" />
              </div>
              <div class="si-fld full">
                <label>جهة التنفيذ <span class="req">*</span></label>
                <select [ngModel]="executingAgency()" (ngModelChange)="executingAgency.set($event)">
                  <option value="">— اختر —</option>
                  @for (a of agencies; track a) { <option [value]="a">{{ a }}</option> }
                </select>
              </div>
              <div class="si-fld full">
                <label>كود المشروع الرئيسي @if (!edit()) { <span class="req">*</span> }</label>
                <input [ngModel]="code()" (ngModelChange)="code.set($event)" [disabled]="!!edit()" placeholder="P-2627-XXX" />
                <div class="hint">{{ edit() ? 'الكود لا يمكن تعديله بعد الإنشاء.' : 'يُحدَّد فور الإنشاء ويجب أن يكون فريدًا.' }}</div>
              </div>
            </div>
          </div>

          <div class="si-modal-foot">
            <button class="si-btn primary" [disabled]="saving()" (click)="submit()">
              @if (saving()) { <span class="mini-sp"></span> جاري الحفظ… } @else { {{ edit() ? 'حفظ التعديلات' : 'إضافة المشروع الرئيسي' }} }
            </button>
            <button class="si-btn" (click)="close.emit()">إلغاء</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`.mini-sp{width:14px;height:14px;border:2px solid rgba(255,255,255,.4);border-top-color:#fff;border-radius:50%;animation:spin .7s linear infinite;display:inline-block}@keyframes spin{to{transform:rotate(360deg)}}`],
})
export class MainProjectForm {
  private readonly projectsService = inject(ProjectsService);
  private readonly lookups = inject(LookupsService);

  readonly open = input(false);
  readonly edit = input<MainProjectListItem | null>(null);
  readonly close = output<void>();
  readonly saved = output<void>();

  protected readonly programs = signal<Lookup[]>([]);
  protected readonly subPrograms = signal<SubProgramLookup[]>([]);
  protected readonly programId = signal<number | null>(null);
  protected readonly subProgramId = signal<number | null>(null);
  protected readonly code = signal('');
  protected readonly name = signal('');
  protected readonly executingAgency = signal('');
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly agencies = EXECUTING_AGENCIES;

  protected readonly filteredSubPrograms = computed(() => {
    const pid = this.programId();
    return this.subPrograms().filter((s) => pid == null || s.mainProgramId === pid);
  });

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

  protected onProgramChange(value: number | null): void {
    this.programId.set(value);
    this.subProgramId.set(null);
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
    forkJoin({ programs: this.lookups.getMainPrograms(), subs: this.lookups.getSubPrograms() }).subscribe({
      next: ({ programs, subs }) => {
        this.programs.set(programs);
        this.subPrograms.set(subs);
        this.lookupsLoaded = true;
        done();
      },
      error: () => this.error.set('تعذّر تحميل القوائم'),
    });
  }

  private prefill(): void {
    const e = this.edit();
    if (e) {
      const sp = this.subPrograms().find((s) => s.id === e.subProgramId);
      this.programId.set(sp?.mainProgramId ?? null);
      this.subProgramId.set(e.subProgramId);
      this.code.set(e.code);
      this.name.set(e.name);
      this.executingAgency.set(e.executingAgency ?? '');
    } else {
      this.programId.set(null);
      this.subProgramId.set(null);
      this.code.set('');
      this.name.set('');
      this.executingAgency.set('');
    }
  }

  protected submit(): void {
    if (this.saving()) return;
    this.error.set(null);

    if (!this.name().trim() || this.subProgramId() == null) {
      this.error.set('برجاء إدخال اسم المشروع واختيار البرنامج الفرعي');
      return;
    }
    if (!this.executingAgency()) {
      this.error.set('برجاء اختيار جهة التنفيذ');
      return;
    }
    if (!this.edit() && !this.code().trim()) {
      this.error.set('برجاء إدخال كود المشروع الرئيسي');
      return;
    }

    this.saving.set(true);
    const dto = {
      code: this.code().trim(),
      name: this.name().trim(),
      executingAgency: this.executingAgency(),
      subProgramId: this.subProgramId()!,
    };
    const editing = this.edit();
    const req = editing
      ? this.projectsService.updateMainProject(editing.id, dto)
      : this.projectsService.createMainProject(dto);

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.emit();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'تعذّر حفظ المشروع');
      },
    });
  }
}
