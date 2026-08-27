import { Component, HostListener, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { FinancialService } from '../../core/services/financial.service';
import { FinancialYearsService } from '../../core/services/financial-years.service';
import {
  CONTRACTING_METHODS,
  PROCUREMENT_STAGE_NAMES,
  ProcurementSubProjectListItem,
} from '../../core/models/financial.models';
import { FinancialYear } from '../../core/models/project.models';

@Component({
  selector: 'app-financial-list',
  imports: [RouterLink, FormsModule],
  templateUrl: './financial-list.html',
  styleUrl: './financial.css',
})
export class FinancialList implements OnInit {
  private readonly financial = inject(FinancialService);
  private readonly financialYearsService = inject(FinancialYearsService);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly items = signal<ProcurementSubProjectListItem[]>([]);
  protected readonly search = signal('');

  protected readonly financialYears = signal<FinancialYear[]>([]);
  protected readonly selectedYearId = signal<number | null>(null);
  protected readonly yearsLoading = signal(true);
  protected readonly yearsError = signal(false);
  protected readonly sortedYears = computed(() =>
    [...this.financialYears()].sort((a, b) => b.startDate.localeCompare(a.startDate)),
  );

  /**
   * فلتر المرحلة الحالية — القيمة هي عدد المراحل المكتملة (0..6)،
   * وتُترجم إلى اسم المرحلة التي يقف عندها المشروع الآن. 6 = اكتمل الطرح.
   * اختيار متعدد؛ مجموعة فارغة تعني بدون فلترة.
   */
  protected readonly stageCountFilter = signal<Set<number>>(new Set());
  protected readonly stageCountOptions = [0, 1, 2, 3, 4, 5, 6];

  /** اسم المرحلة التي يقف عندها المشروع = المرحلة التالية لآخر مرحلة مكتملة */
  protected stageLabel(completedStages: number): string {
    return PROCUREMENT_STAGE_NAMES[completedStages] ?? 'اكتمل الطرح';
  }

  /**
   * اسم المرحلة المعروض في عمود "تقدم الطرح" لكل صف — عرض فقط، لا يغيّر completedStages/totalStages:
   * مشروع بلا مذكرة عرض لم يبدأ فعليًا "كراسة الشروط" (محظور عليه حتى يُرفَق له مذكرة معتمدة)،
   * فيُعرض واقفًا عند "مذكرة عرض" بدلًا من الإيحاء بأنه بدأ الطرح فعلًا.
   */
  protected rowStageLabel(item: ProcurementSubProjectListItem): string {
    if (item.completedStages === 0 && !item.hasPresentationMemo) {
      return 'مذكرة عرض';
    }
    return this.stageLabel(item.completedStages);
  }

  /** فلتر نوع التعاقد — مأخوذ من مذكرة العرض الفعّالة */
  protected readonly methodFilter = signal<Set<number>>(new Set());

  protected toggleMethod(value: number): void {
    this.methodFilter.update((current) => {
      const next = new Set(current);
      if (next.has(value)) {
        next.delete(value);
      } else {
        next.add(value);
      }
      return next;
    });
  }

  /** فلتر وجود مذكرة عرض */
  protected readonly memoFilter = signal<'all' | 'with' | 'without'>('all');

  /** الصفوف بعد البحث فقط — الأساس الذي تُحسب عليه أعداد الفلاتر */
  private readonly searchMatched = computed(() => {
    const term = this.search().trim();
    if (!term) {
      return this.items();
    }
    return this.items().filter(
      (x) =>
        x.subProjectName.includes(term) ||
        (x.subProjectCode ?? '').includes(term) ||
        x.mainProjectName.includes(term),
    );
  });

  /**
   * العدّادات تُحسب على نتائج البحث بعد تطبيق فلاتر المجموعات الأخرى فقط،
   * حتى يعكس الرقم بجوار كل خيار عدد الصفوف التي سيعطيها اختياره فعلًا.
   *
   * محسوبة دفعة واحدة في خرائط لأن استدعاءها كدوال داخل @for يعيد مسح
   * القائمة مع كل دورة كشف تغيير.
   */
  private readonly stageCounts = computed(() => {
    const counts = new Map<number, number>();
    for (const x of this.searchMatched()) {
      if (this.matchesMethod(x) && this.matchesMemo(x)) {
        counts.set(x.completedStages, (counts.get(x.completedStages) ?? 0) + 1);
      }
    }
    return counts;
  });

  private readonly methodCounts = computed(() => {
    const counts = new Map<number, number>();
    for (const x of this.searchMatched()) {
      if (x.contractingMethod != null && this.matchesStage(x) && this.matchesMemo(x)) {
        counts.set(x.contractingMethod, (counts.get(x.contractingMethod) ?? 0) + 1);
      }
    }
    return counts;
  });

  private readonly memoCounts = computed(() => {
    let withMemo = 0;
    let withoutMemo = 0;
    for (const x of this.searchMatched()) {
      if (!this.matchesStage(x) || !this.matchesMethod(x)) {
        continue;
      }
      if (x.hasPresentationMemo) {
        withMemo++;
      } else {
        withoutMemo++;
      }
    }
    return { with: withMemo, without: withoutMemo };
  });

  protected stageCountFor(completedStages: number): number {
    return this.stageCounts().get(completedStages) ?? 0;
  }

  protected methodCountFor(value: number): number {
    return this.methodCounts().get(value) ?? 0;
  }

  protected memoCountFor(mode: 'with' | 'without'): number {
    return this.memoCounts()[mode];
  }

  /** كل أنواع التعاقد السبعة تظهر دائمًا — النوع بلا مشروعات يظهر بعدّاد صفر بدل الاختفاء */
  protected readonly allMethods = CONTRACTING_METHODS;

  protected selectedMethodLabel(): string {
    const value = this.methodFilter().values().next().value;
    return this.allMethods.find((m) => m.value === value)?.label ?? '';
  }

  private matchesStage(x: ProcurementSubProjectListItem): boolean {
    const stages = this.stageCountFilter();
    return stages.size === 0 || stages.has(x.completedStages);
  }

  private matchesMethod(x: ProcurementSubProjectListItem): boolean {
    const methods = this.methodFilter();
    return methods.size === 0 || (x.contractingMethod != null && methods.has(x.contractingMethod));
  }

  private matchesMemo(x: ProcurementSubProjectListItem): boolean {
    const mode = this.memoFilter();
    return mode === 'all' || x.hasPresentationMemo === (mode === 'with');
  }

  /**
   * فلترا المرحلة الحالية ونوع التعاقد قائمتان منسدلتان بمربعات اختيار متعدد —
   * الاختيار المتعدد كما هو، فقط شكل العرض تغيّر. قيمة واحدة تتذكر أيهما مفتوحة
   * حتى يُغلق فتح إحداهما الأخرى تلقائيًا.
   */
  protected readonly openDropdown = signal<'stage' | 'method' | null>(null);

  protected toggleDropdown(which: 'stage' | 'method', event: Event): void {
    event.stopPropagation();
    this.openDropdown.update((current) => (current === which ? null : which));
  }

  /** أي نقرة تصل حتى مستند الصفحة لم تُوقَف داخل زر أو لوحة — أي أنها خارجهما */
  @HostListener('document:click')
  protected closeDropdowns(): void {
    this.openDropdown.set(null);
  }

  protected toggleStageCount(n: number): void {
    this.stageCountFilter.update((current) => {
      const next = new Set(current);
      if (next.has(n)) {
        next.delete(n);
      } else {
        next.add(n);
      }
      return next;
    });
  }

  protected readonly filtered = computed(() =>
    this.searchMatched().filter(
      (x) => this.matchesStage(x) && this.matchesMethod(x) && this.matchesMemo(x),
    ),
  );

  protected readonly hasActiveFilters = computed(
    () =>
      this.stageCountFilter().size > 0 ||
      this.methodFilter().size > 0 ||
      this.memoFilter() !== 'all',
  );

  protected clearFilters(): void {
    this.stageCountFilter.set(new Set());
    this.methodFilter.set(new Set());
    this.memoFilter.set('all');
  }

  // ===== pagination =====
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filtered().length / this.pageSize)),
  );
  protected readonly pagedItems = computed<ProcurementSubProjectListItem[]>(() => {
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

  constructor() {
    effect(() => {
      this.search();
      this.stageCountFilter();
      this.methodFilter();
      this.memoFilter();
      this.page.set(1);
    });
  }

  protected readonly kpiTotal = computed(() => this.items().length);
  protected readonly kpiDone = computed(
    () => this.items().filter((x) => x.completedStages === x.totalStages).length,
  );
  protected readonly kpiActive = computed(
    () => this.items().filter((x) => x.completedStages > 0 && x.completedStages < x.totalStages).length,
  );

  ngOnInit(): void {
    this.financialYearsService.getAll().subscribe({
      next: (years) => {
        this.financialYears.set(years);
        this.yearsLoading.set(false);
        const requestedYear = Number(this.route.snapshot.queryParamMap.get('financialYearId'));
        const preferredYear = Number.isInteger(requestedYear) && requestedYear > 0 ? requestedYear : this.selectedYearId();
        this.selectedYearId.set(
          this.financialYearsService.resolveSelectedYearId(years, preferredYear),
        );
        this.load();
      },
      error: () => {
        this.yearsLoading.set(false);
        this.yearsError.set(true);
        this.load();
      },
    });
  }

  protected onYearChange(id: number | null): void {
    this.selectedYearId.set(id);
    this.financialYearsService.rememberSelectedYearId(id);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.financial.getSubProjects(this.selectedYearId()).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذر تحميل بيانات التعاقدات');
        this.loading.set(false);
      },
    });
  }

  protected progress(item: ProcurementSubProjectListItem): number {
    return item.totalStages === 0 ? 0 : Math.round((item.completedStages / item.totalStages) * 100);
  }
}
