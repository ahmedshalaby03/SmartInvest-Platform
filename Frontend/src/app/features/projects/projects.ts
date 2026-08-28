import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProjectsService } from '../../core/services/projects.service';
import { LookupsService } from '../../core/services/lookups.service';
import { AgenciesService } from '../../core/services/agencies.service';
import { AuthService } from '../../core/services/auth.service';
import { FinancialYearsService } from '../../core/services/financial-years.service';
import { BankAvailabilitiesService } from '../../core/services/bank-availabilities.service';
import { ToastService } from '../../core/services/toast.service';
import { egpToThousands, formatEgpAsThousands, thousandsToEgp } from '../../core/utils/budget.util';
import {
  BankAvailability,
  BankAvailabilityDocument,
  BankAvailabilityList,
  FinancialYear,
  Lookup,
  MainProjectListItem,
  MarkazLookup,
  SubProgramLookup,
  SubProjectListItem,
} from '../../core/models/project.models';
import { MainProjectForm } from './main-project-form';
import { SubProjectForm, LockedParent } from './sub-project-form';
import { ExcelImportWizard } from './excel-import-wizard';

interface MainWithSubs {
  main: MainProjectListItem;
  subs: SubProjectListItem[];
}

@Component({
  selector: 'app-projects',
  imports: [FormsModule, RouterLink, MainProjectForm, SubProjectForm, ExcelImportWizard],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class Projects {
  private readonly projectsService = inject(ProjectsService);
  private readonly lookups = inject(LookupsService);
  private readonly agenciesService = inject(AgenciesService);
  private readonly auth = inject(AuthService);
  private readonly financialYearsService = inject(FinancialYearsService);
  private readonly bankAvailabilitiesService = inject(BankAvailabilitiesService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private pendingOpenAvailability = false;

  protected readonly canEditProjects = this.auth.canEditProjects;
  protected readonly canManageProjects = this.auth.canManageProjects;
  protected readonly canEditFinancial = this.auth.canEditFinancial;
  protected readonly canManageFinancial = this.auth.canManageFinancial;
  protected readonly agencies = signal<string[]>([]);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly mains = signal<MainProjectListItem[]>([]);
  protected readonly subs = signal<SubProjectListItem[]>([]);

  // ===== السنة المالية =====
  protected readonly financialYears = signal<FinancialYear[]>([]);
  protected readonly selectedYearId = signal<number | null>(null);
  protected readonly sortedYears = computed(() =>
    [...this.financialYears()].sort((a, b) => b.startDate.localeCompare(a.startDate)),
  );

  protected readonly showAddYearForm = signal(false);
  protected readonly newYearBudgetThousands = signal<number | null>(null);
  protected readonly newYearBudgetTotal = computed(() => thousandsToEgp(this.newYearBudgetThousands()));
  protected readonly addYearError = signal<string | null>(null);
  protected readonly savingYear = signal(false);

  // ابحث + فلاتر
  protected readonly searchTerm = signal('');
  protected readonly approvalFilter = signal<'all' | 'approved' | 'pending' | 'stalled'>('all');
  protected readonly showAdvanced = signal(false);
  protected readonly fMainProgram = signal('');
  protected readonly fSubProgram = signal('');
  protected readonly fLevel = signal('');
  protected readonly fProjectNature = signal('');
  protected readonly fAgency = signal('');
  protected readonly fMarkaz = signal('');
  protected readonly fPriority = signal('');
  protected readonly fFunding = signal('');

  // قوائم الفلاتر
  protected readonly mainPrograms = signal<Lookup[]>([]);
  protected readonly subPrograms = signal<SubProgramLookup[]>([]);
  protected readonly markazList = signal<MarkazLookup[]>([]);
  protected readonly priorities = signal<Lookup[]>([]);
  protected readonly projectLevels = signal<Lookup[]>([]);

  // ===== مؤشرات =====
  protected readonly kpiTotal = computed(() => this.subs().length);
  protected readonly kpiApproved = computed(() => this.subs().filter((s) => s.isApproved && !this.isStalled(s)).length);
  protected readonly kpiPending = computed(() => this.subs().filter((s) => !s.isApproved).length);
  protected readonly kpiStalled = computed(() => this.subs().filter((s) => this.isStalled(s)).length);
  // المشروعات المعتمدة فقط — تمويل مشروع مقترح لم يُعتمد بعد لا يُحسب ضمن الإجماليات المالية.
  protected readonly kpiBank = computed(() => this.subs().filter((s) => s.isApproved).reduce((a, s) => a + s.bankFunding, 0));
  protected readonly kpiSelf = computed(() => this.subs().filter((s) => s.isApproved).reduce((a, s) => a + s.selfFunding, 0));
  protected readonly kpiTotalFunding = computed(() => this.kpiBank() + this.kpiSelf());
  protected readonly kpiAvailable = computed(() => this.availabilityData()?.totalAvailable ?? 0);

  protected isStalled(s: SubProjectListItem): boolean {
    return s.isApproved && s.statusName === 'متعثر';
  }

  // ===== الإتاحات البنكية =====
  protected readonly availabilityData = signal<BankAvailabilityList | null>(null);
  protected readonly availabilityLoading = signal(false);
  protected readonly availabilityError = signal<string | null>(null);
  protected readonly selectedYear = computed(() => this.financialYears().find((y) => y.id === this.selectedYearId()) ?? null);
  protected readonly canDeleteAvailability = this.auth.canManageFinancial;
  protected readonly canAddAvailability = this.auth.canAddBankAvailability;

  protected readonly showAvailabilityModal = signal(false);
  protected readonly showAddAvailabilityForm = signal(false);
  protected readonly editingAvailability = signal<BankAvailability | null>(null);
  protected readonly newAvailabilityThousands = signal<number | null>(null);
  protected readonly newAvailabilityTotal = computed(() => thousandsToEgp(this.newAvailabilityThousands()));
  protected readonly newAvailabilityDate = signal('');
  protected readonly newAvailabilityNotes = signal('');
  protected readonly newAvailabilityFiles = signal<File[]>([]);
  protected readonly keptDocuments = signal<BankAvailabilityDocument[]>([]);
  protected readonly finalDocumentCount = computed(() => this.keptDocuments().length + this.newAvailabilityFiles().length);
  protected readonly addAvailabilityError = signal<string | null>(null);
  protected readonly savingAvailability = signal(false);
  protected readonly dragActive = signal(false);

  protected readonly deleteTarget = signal<BankAvailability | null>(null);
  protected readonly deletingAvailability = signal(false);

  private loadAvailability(): void {
    const yearId = this.selectedYearId();
    if (yearId == null) {
      this.availabilityData.set(null);
      return;
    }
    this.availabilityLoading.set(true);
    this.availabilityError.set(null);
    this.bankAvailabilitiesService.getForFinancialYear(yearId).subscribe({
      next: (data) => {
        this.availabilityData.set(data);
        this.availabilityLoading.set(false);
      },
      error: () => {
        this.availabilityError.set('تعذّر تحميل الإتاحات البنكية');
        this.availabilityLoading.set(false);
      },
    });
  }

  protected openAvailabilityModal(): void {
    if (this.selectedYearId() == null) return;
    this.showAvailabilityModal.set(true);
    this.loadAvailability();
  }

  protected closeAvailabilityModal(): void {
    if (this.savingAvailability() || this.deletingAvailability()) return;
    this.showAvailabilityModal.set(false);
    this.showAddAvailabilityForm.set(false);
    this.editingAvailability.set(null);
    this.deleteTarget.set(null);
  }

  protected openAddAvailabilityForm(): void {
    this.editingAvailability.set(null);
    this.newAvailabilityThousands.set(null);
    this.newAvailabilityDate.set('');
    this.newAvailabilityNotes.set('');
    this.newAvailabilityFiles.set([]);
    this.keptDocuments.set([]);
    this.addAvailabilityError.set(null);
    this.showAddAvailabilityForm.set(true);
  }

  protected openEditAvailabilityForm(a: BankAvailability): void {
    this.editingAvailability.set(a);
    this.newAvailabilityThousands.set(egpToThousands(a.amount));
    this.newAvailabilityDate.set(this.dateOnly(a.receivedDate));
    this.newAvailabilityNotes.set(a.notes ?? '');
    this.newAvailabilityFiles.set([]);
    this.keptDocuments.set([...a.documents]);
    this.addAvailabilityError.set(null);
    this.showAddAvailabilityForm.set(true);
  }

  protected closeAddAvailabilityForm(): void {
    if (this.savingAvailability()) return;
    this.showAddAvailabilityForm.set(false);
    this.editingAvailability.set(null);
  }

  protected updateNewAvailabilityThousands(value: number | string): void {
    if (value === '' || value == null) {
      this.newAvailabilityThousands.set(null);
      return;
    }
    const num = Number(value);
    this.newAvailabilityThousands.set(Number.isNaN(num) || num < 0 ? null : num);
  }

  protected removeKeptDocument(documentId: number): void {
    this.keptDocuments.update((list) => list.filter((d) => d.id !== documentId));
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(true);
  }

  protected onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
  }

  protected onFileDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    if (event.dataTransfer?.files?.length) {
      this.addAvailabilityFiles(Array.from(event.dataTransfer.files));
    }
  }

  protected onAvailabilityFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.addAvailabilityFiles(Array.from(input.files));
    }
    input.value = '';
  }

  private addAvailabilityFiles(files: File[]): void {
    this.newAvailabilityFiles.update((existing) => [...existing, ...files]);
  }

  protected removeAvailabilityFile(index: number): void {
    this.newAvailabilityFiles.update((files) => files.filter((_, i) => i !== index));
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} بايت`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} كيلوبايت`;
    return `${(bytes / 1024 / 1024).toFixed(2)} ميجابايت`;
  }

  protected submitAvailability(): void {
    if (this.savingAvailability()) return;
    const yearId = this.selectedYearId();
    if (yearId == null) return;

    const amount = this.newAvailabilityTotal();
    if (amount <= 0) {
      this.addAvailabilityError.set('برجاء إدخال قيمة إتاحة أكبر من صفر');
      return;
    }
    if (!this.newAvailabilityDate()) {
      this.addAvailabilityError.set('برجاء إدخال تاريخ استلام الإتاحة');
      return;
    }
    if (this.finalDocumentCount() === 0) {
      this.addAvailabilityError.set('برجاء إرفاق مستند إثبات واحد على الأقل');
      return;
    }

    const formData = new FormData();
    formData.set('amount', String(amount));
    formData.set('receivedDate', this.newAvailabilityDate());
    if (this.newAvailabilityNotes().trim()) {
      formData.set('notes', this.newAvailabilityNotes().trim());
    }
    for (const file of this.newAvailabilityFiles()) {
      formData.append('documents', file, file.name);
    }

    const editing = this.editingAvailability();
    this.savingAvailability.set(true);
    this.addAvailabilityError.set(null);

    if (editing) {
      for (const doc of this.keptDocuments()) {
        formData.append('keepDocumentIds', String(doc.id));
      }
      this.bankAvailabilitiesService.update(yearId, editing.id, formData).subscribe({
        next: () => {
          this.savingAvailability.set(false);
          this.showAddAvailabilityForm.set(false);
          this.editingAvailability.set(null);
          this.toast.success('تم حفظ تعديلات الإتاحة البنكية');
          this.loadAvailability();
        },
        error: (err) => {
          this.savingAvailability.set(false);
          this.addAvailabilityError.set(err?.error?.message ?? 'تعذّر حفظ تعديلات الإتاحة');
        },
      });
      return;
    }

    this.bankAvailabilitiesService.create(yearId, formData).subscribe({
      next: () => {
        this.savingAvailability.set(false);
        this.showAddAvailabilityForm.set(false);
        this.toast.success('تم إضافة الإتاحة البنكية');
        this.loadAvailability();
      },
      error: (err) => {
        this.savingAvailability.set(false);
        this.addAvailabilityError.set(err?.error?.message ?? 'تعذّر إضافة الإتاحة البنكية');
      },
    });
  }

  protected downloadAvailabilityDocument(availabilityId: number, doc: BankAvailabilityDocument): void {
    const yearId = this.selectedYearId();
    if (yearId == null) return;
    this.bankAvailabilitiesService
      .downloadDocument(yearId, availabilityId, doc.id)
      .subscribe((blob) => this.bankAvailabilitiesService.saveBlob(blob, doc.fileName));
  }

  protected openDeleteConfirm(a: BankAvailability): void {
    this.deleteTarget.set(a);
  }

  protected closeDeleteConfirm(): void {
    if (this.deletingAvailability()) return;
    this.deleteTarget.set(null);
  }

  protected confirmDeleteAvailability(): void {
    if (this.deletingAvailability()) return;
    const target = this.deleteTarget();
    const yearId = this.selectedYearId();
    if (!target || yearId == null) return;

    this.deletingAvailability.set(true);
    this.bankAvailabilitiesService.delete(yearId, target.id).subscribe({
      next: () => {
        this.deletingAvailability.set(false);
        this.deleteTarget.set(null);
        this.toast.success('تم حذف الإتاحة البنكية');
        this.loadAvailability();
      },
      error: (err) => {
        this.deletingAvailability.set(false);
        this.toast.error(err?.error?.message ?? 'تعذّر حذف الإتاحة البنكية');
      },
    });
  }

  // ===== الفلترة والتجميع =====
  private matchesSubFilters(s: SubProjectListItem): boolean {
    if (this.approvalFilter() === 'approved' && (!s.isApproved || this.isStalled(s))) return false;
    if (this.approvalFilter() === 'pending' && s.isApproved) return false;
    if (this.approvalFilter() === 'stalled' && !this.isStalled(s)) return false;
    if (this.fLevel() && s.projectLevelName !== this.fLevel()) return false;
    if (this.fProjectNature() && s.projectNature !== this.fProjectNature()) return false;
    if (this.fAgency() && s.executiveAgencyName !== this.fAgency()) return false;
    if (this.fMarkaz() && String(s.markazId) !== this.fMarkaz()) return false;
    if (this.fPriority() && String(s.priorityId) !== this.fPriority()) return false;
    // تصنيف حصري: "بنكي فقط"/"ذاتي فقط" يستبعدان أي مبلغ من المصدر الآخر، و"مختلط" يشترط الاثنين معًا —
    // وليس مجرد "فيه مبلغ من هذا المصدر ولو جزئيًا"، وإلا يطابق "بنكي" شبه كل المشروعات لأن أغلبها له تمويل بنكي جزئي.
    if (this.fFunding() === 'bank' && !(s.bankFunding > 0 && s.selfFunding <= 0)) return false;
    if (this.fFunding() === 'self' && !(s.selfFunding > 0 && s.bankFunding <= 0)) return false;
    if (this.fFunding() === 'mixed' && !(s.bankFunding > 0 && s.selfFunding > 0)) return false;
    return true;
  }

  private matchesMainFilters(m: MainProjectListItem): boolean {
    if (this.fMainProgram() && m.mainProgramName !== this.fMainProgram()) return false;
    if (this.fSubProgram() && m.subProgramName !== this.fSubProgram()) return false;
    return true;
  }

  // ===== الفلترة: مشروع رئيسي ← فرعيات =====
  protected readonly filteredMains = computed<MainWithSubs[]>(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const subsByMain = new Map<number, SubProjectListItem[]>();
    for (const s of this.subs()) {
      const arr = subsByMain.get(s.mainProjectId) ?? [];
      arr.push(s);
      subsByMain.set(s.mainProjectId, arr);
    }

    const mains = [...this.mains()].sort(
      (a, b) => (a.code ?? '').localeCompare(b.code ?? '', 'ar') || a.name.localeCompare(b.name, 'ar'),
    );

    const rows: MainWithSubs[] = [];
    for (const m of mains) {
      if (!subsByMain.has(m.id)) continue;
      if (!this.matchesMainFilters(m)) continue;

      let mySubs = (subsByMain.get(m.id) ?? []).filter((s) => this.matchesSubFilters(s));

      if (term) {
        const mainHit =
          m.name.toLowerCase().includes(term) || (m.code ?? '').toLowerCase().includes(term);
        const subHits = mySubs.filter(
          (s) => s.name.toLowerCase().includes(term) || (s.code ?? '').toLowerCase().includes(term),
        );
        if (!mainHit && subHits.length === 0) continue;
        if (!mainHit) mySubs = subHits;
      }

      // The subsByMain.has(m.id) check above only guarantees this main has subs in the SELECTED
      // YEAR - it says nothing about the approval/level/agency/etc filters just applied. If none
      // of this main's subs survived matchesSubFilters, the main itself has nothing to show under
      // the active filters and must be hidden entirely, not kept with an empty sub-list (which
      // rendered as a misleading "لا توجد مشاريع فرعية" row while still counting toward the
      // "N مشروع رئيسي" total).
      if (mySubs.length === 0) continue;

      rows.push({ main: m, subs: mySubs });
    }

    return rows;
  });

  // ===== الطي والفتح (accordion) للمشروع الرئيسي - مفتوح افتراضيًا =====
  protected readonly collapsedProjects = signal<Set<number>>(new Set());

  protected toggleProject(projectId: number): void {
    const next = new Set(this.collapsedProjects());
    if (next.has(projectId)) {
      next.delete(projectId);
    } else {
      next.add(projectId);
    }
    this.collapsedProjects.set(next);
  }

  protected isProjectExpanded(projectId: number): boolean {
    return !this.collapsedProjects().has(projectId);
  }

  // ===== pagination على المشاريع الرئيسية =====
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredMains().length / this.pageSize)),
  );
  protected readonly pagedMains = computed<MainWithSubs[]>(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredMains().slice(start, start + this.pageSize);
  });
  protected readonly rangeStart = computed(() =>
    this.filteredMains().length === 0 ? 0 : (this.page() - 1) * this.pageSize + 1,
  );
  protected readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize, this.filteredMains().length),
  );

  protected goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages()) this.page.set(p);
  }

  // ===== modals =====
  protected readonly showMainForm = signal(false);
  protected readonly mainEdit = signal<MainProjectListItem | null>(null);
  protected readonly showSubForm = signal(false);
  protected readonly subEdit = signal<SubProjectListItem | null>(null);
  protected readonly subLocked = signal<LockedParent | null>(null);
  protected readonly showImportWizard = signal(false);

  constructor() {
    this.applyQueryParams();
    this.loadFinancialYears();
    this.loadLookups();
    effect(() => {
      this.searchTerm();
      this.approvalFilter();
      this.fMainProgram(); this.fSubProgram(); this.fLevel();
      this.fProjectNature(); this.fAgency(); this.fMarkaz(); this.fPriority(); this.fFunding();
      this.page.set(1);
    });
  }

  /** يقرأ financialYearId / approval / openAvailability من رابط الدخول (مثل روابط لوحة التحكم). */
  private applyQueryParams(): void {
    const params = this.route.snapshot.queryParamMap;

    const yearIdRaw = params.get('financialYearId');
    const yearId = yearIdRaw != null ? Number(yearIdRaw) : NaN;
    if (!Number.isNaN(yearId)) {
      this.selectedYearId.set(yearId);
    }

    const approval = params.get('approval');
    if (approval === 'approved' || approval === 'pending' || approval === 'stalled') {
      this.approvalFilter.set(approval);
    }

    this.pendingOpenAvailability = params.get('openAvailability') === 'true';
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.loadAvailability();
    forkJoin({
      mains: this.projectsService.getMainProjects(),
      subs: this.projectsService.searchSubProjects({
        page: 1,
        pageSize: 1000,
        financialYearId: this.selectedYearId() ?? undefined,
      }),
    }).subscribe({
      next: ({ mains, subs }) => {
        this.mains.set(mains);
        this.subs.set(subs.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل المشروعات. تأكد من تشغيل الخادم وتسجيل الدخول.');
        this.loading.set(false);
      },
    });
  }

  private loadFinancialYears(): void {
    this.financialYearsService.getAll().subscribe({
      next: (years) => {
        this.financialYears.set(years);
        this.selectedYearId.set(
          this.financialYearsService.resolveSelectedYearId(years, this.selectedYearId()),
        );
        this.load();
        if (this.pendingOpenAvailability) {
          this.pendingOpenAvailability = false;
          if (this.selectedYearId() != null) {
            this.openAvailabilityModal();
          }
        }
      },
      error: () => {
        this.load();
      },
    });
  }

  protected onYearChange(id: number): void {
    this.selectedYearId.set(id);
    this.financialYearsService.rememberSelectedYearId(id);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { financialYearId: id },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.load();
  }

  private loadLookups(): void {
    this.lookups.getMainPrograms().subscribe((d) => this.mainPrograms.set(d));
    this.lookups.getSubPrograms().subscribe((d) => this.subPrograms.set(d));
    this.lookups.getMarkaz().subscribe((d) => this.markazList.set(d));
    this.lookups.getPriorities().subscribe((d) => this.priorities.set(d));
    this.lookups.getProjectLevels().subscribe((d) => this.projectLevels.set(d));
    this.agenciesService.getAll().subscribe((d) => this.agencies.set(d.map((a) => a.agencyName)));
  }

  protected clearFilters(): void {
    this.fMainProgram.set(''); this.fSubProgram.set(''); this.fLevel.set(''); this.fProjectNature.set('');
    this.fAgency.set(''); this.fMarkaz.set(''); this.fPriority.set(''); this.fFunding.set('');
    this.searchTerm.set(''); this.approvalFilter.set('all');
  }

  protected money(value: number): string {
    return (value ?? 0).toLocaleString('en-US');
  }

  protected thousandsLabel(value: number): string {
    return formatEgpAsThousands(value);
  }

  protected thousandsValue(value: number): string {
    return (egpToThousands(value) ?? 0).toLocaleString('en-US', { maximumFractionDigits: 3 });
  }

  protected dateOnly(value: string): string {
    return value ? value.slice(0, 10) : '';
  }

  // ===== modals actions =====
  protected openEditMain(m: MainProjectListItem): void { this.mainEdit.set(m); this.showMainForm.set(true); }
  protected openAddSub(): void { this.subEdit.set(null); this.subLocked.set(null); this.showSubForm.set(true); }
  protected openAddSubFor(m: MainProjectListItem): void {
    this.subEdit.set(null);
    this.subLocked.set({ id: m.id, code: m.code, name: m.name });
    this.showSubForm.set(true);
  }
  protected openEditSub(s: SubProjectListItem): void { this.subEdit.set(s); this.subLocked.set(null); this.showSubForm.set(true); }
  protected closeModals(): void { this.showMainForm.set(false); this.showSubForm.set(false); }
  protected onSaved(): void { this.closeModals(); this.load(); }
  protected openImportExcel(): void { this.showImportWizard.set(true); }
  protected closeImportWizard(): void { this.showImportWizard.set(false); }
  protected onImportSaved(): void { this.showImportWizard.set(false); this.load(); }

  protected deleteMain(m: MainProjectListItem): void {
    if (!confirm(`تأكيد حذف المشروع الرئيسي «${m.name}»؟`)) return;
    this.projectsService.deleteMainProject(m.id).subscribe({
      next: () => { this.closeModals(); this.load(); },
      error: (err) => alert(err?.error?.message ?? 'تعذّر حذف المشروع'),
    });
  }
  protected deleteSub(s: SubProjectListItem): void {
    if (!confirm(`تأكيد حذف المشروع الفرعي «${s.name}»؟`)) return;
    this.projectsService.deleteSubProject(s.id).subscribe({
      next: () => { this.closeModals(); this.load(); },
      error: (err) => alert(err?.error?.message ?? 'تعذّر حذف المشروع الفرعي'),
    });
  }

  // ===== إضافة سنة مالية =====
  protected computeNextYear(): { name: string; startDate: string; endDate: string } {
    const latest = this.sortedYears()[0];
    let start: Date;
    if (latest) {
      start = new Date(latest.endDate);
      start.setDate(start.getDate() + 1);
    } else {
      start = new Date();
    }
    const end = new Date(start);
    end.setFullYear(end.getFullYear() + 1);
    end.setDate(end.getDate() - 1);
    return {
      name: `${start.getFullYear()}/${end.getFullYear()}`,
      startDate: this.toLocalIsoDate(start),
      endDate: this.toLocalIsoDate(end),
    };
  }

  private toLocalIsoDate(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  protected openAddYear(): void {
    this.newYearBudgetThousands.set(null);
    this.addYearError.set(null);
    this.showAddYearForm.set(true);
  }

  protected closeAddYear(): void {
    this.showAddYearForm.set(false);
  }

  protected updateNewYearBudgetThousands(value: number | string): void {
    if (value === '' || value == null) {
      this.newYearBudgetThousands.set(null);
      return;
    }
    const num = Number(value);
    this.newYearBudgetThousands.set(Number.isNaN(num) || num < 0 ? null : num);
  }

  protected confirmAddYear(): void {
    if (this.savingYear()) return;
    const next = this.computeNextYear();
    this.savingYear.set(true);
    this.addYearError.set(null);
    const budget = this.newYearBudgetTotal();
    this.financialYearsService
      .create({ name: next.name, startDate: next.startDate, endDate: next.endDate, budget: budget > 0 ? budget : null })
      .subscribe({
        next: (year) => {
          this.savingYear.set(false);
          this.showAddYearForm.set(false);
          this.financialYears.update((list) => [...list, year]);
          this.selectedYearId.set(year.id);
          this.financialYearsService.rememberSelectedYearId(year.id);
          this.load();
        },
        error: (err) => {
          this.savingYear.set(false);
          this.addYearError.set(err?.error?.message ?? 'تعذّر إضافة السنة المالية');
        },
      });
  }

  // ===== اعتماد المشروع (رئيسي أو فرعي) =====
  protected readonly showApprovalModal = signal(false);
  protected readonly approvalTarget = signal<{ kind: 'main' | 'sub'; id: number; name: string } | null>(null);
  protected readonly approvalCode = signal('');
  protected readonly approvalSaving = signal(false);
  protected readonly approvalError = signal<string | null>(null);

  protected openMainApprovalModal(m: MainProjectListItem, event: Event): void {
    event.stopPropagation();
    this.approvalTarget.set({ kind: 'main', id: m.id, name: m.name });
    this.approvalCode.set('');
    this.approvalError.set(null);
    this.showApprovalModal.set(true);
  }

  protected openSubApprovalModal(s: SubProjectListItem, event: Event): void {
    event.stopPropagation();
    this.approvalTarget.set({ kind: 'sub', id: s.id, name: s.name });
    this.approvalCode.set('');
    this.approvalError.set(null);
    this.showApprovalModal.set(true);
  }

  protected closeApprovalModal(): void {
    this.showApprovalModal.set(false);
    this.approvalTarget.set(null);
  }

  protected submitApproval(): void {
    if (this.approvalSaving()) return;
    this.approvalError.set(null);

    const target = this.approvalTarget();
    if (!target) return;

    const code = this.approvalCode().trim();
    if (!code) {
      this.approvalError.set('برجاء إدخال كود المشروع للاعتماد');
      return;
    }

    this.approvalSaving.set(true);

    if (target.kind === 'sub') {
      this.projectsService.approveSubProject(target.id, code).subscribe({
        next: () => this.onApprovalSaved(),
        error: (err) => this.onApprovalError(err, 'تعذّر اعتماد المشروع الفرعي'),
      });
      return;
    }

    const m = this.mains().find((x) => x.id === target.id);
    if (!m) {
      this.approvalSaving.set(false);
      this.approvalError.set('تعذّر إيجاد بيانات المشروع الرئيسي');
      return;
    }

    this.projectsService
      .updateMainProject(target.id, {
        code,
        name: m.name,
        executingAgency: m.executingAgency,
        subProgramId: m.subProgramId,
      })
      .subscribe({
        next: () => this.onApprovalSaved(),
        error: (err) => this.onApprovalError(err, 'تعذّر اعتماد المشروع الرئيسي'),
      });
  }

  private onApprovalSaved(): void {
    this.approvalSaving.set(false);
    this.showApprovalModal.set(false);
    this.approvalTarget.set(null);
    this.load();
  }

  private onApprovalError(err: unknown, fallback: string): void {
    this.approvalSaving.set(false);
    this.approvalError.set((err as { error?: { message?: string } })?.error?.message ?? fallback);
  }
}
