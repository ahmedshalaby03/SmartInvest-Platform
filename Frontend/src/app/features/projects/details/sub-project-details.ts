import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProjectsService } from '../../../core/services/projects.service';
import { SpecificationsService } from '../../../core/services/specifications.service';
import { AuthService } from '../../../core/services/auth.service';
import { SubProjectDetail, SubProjectListItem } from '../../../core/models/project.models';
import { ProjectSpecification } from '../../../core/models/specification.models';

type Tab = 'basic' | 'specs' | 'location' | 'subs';

@Component({
  selector: 'app-sub-project-details',
  imports: [FormsModule, RouterLink],
  templateUrl: './sub-project-details.html',
  styleUrl: './sub-project-details.css',
})
export class SubProjectDetails {
  private readonly route = inject(ActivatedRoute);
  private readonly projectsService = inject(ProjectsService);
  private readonly specsService = inject(SpecificationsService);
  private readonly auth = inject(AuthService);

  protected readonly isManager = this.auth.isManager;

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly project = signal<SubProjectDetail | null>(null);
  protected readonly tab = signal<Tab>('basic');

  // المواصفات
  protected readonly specs = signal<ProjectSpecification[]>([]);
  protected readonly specsLoaded = signal(false);
  // المشاريع الفرعية الشقيقة
  protected readonly siblings = signal<SubProjectListItem[]>([]);
  protected readonly siblingsLoaded = signal(false);

  // نموذج إضافة/تعديل مواصفة
  protected readonly showSpecForm = signal(false);
  protected readonly editingSpecId = signal<number | null>(null);
  protected readonly sName = signal('');
  protected readonly sValue = signal('');
  protected readonly sUnit = signal('');
  protected readonly savingSpec = signal(false);

  private subId = 0;

  protected readonly isApproved = computed(() => !!this.project()?.code);

  constructor() {
    this.subId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.projectsService.getSubProject(this.subId).subscribe({
      next: (data) => {
        this.project.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل تفاصيل المشروع.');
        this.loading.set(false);
      },
    });
  }

  protected setTab(t: Tab): void {
    this.tab.set(t);
    if (t === 'specs' && !this.specsLoaded()) {
      this.loadSpecs();
    }
    if (t === 'subs' && !this.siblingsLoaded()) {
      this.loadSiblings();
    }
  }

  private loadSpecs(): void {
    this.specsService.getAll(this.subId).subscribe({
      next: (data) => {
        this.specs.set(data);
        this.specsLoaded.set(true);
      },
    });
  }

  private loadSiblings(): void {
    const mainId = this.project()?.mainProjectId;
    if (!mainId) {
      return;
    }
    this.projectsService.getMainProject(mainId).subscribe({
      next: (detail) => {
        this.siblings.set(detail.subProjects);
        this.siblingsLoaded.set(true);
      },
    });
  }

  protected money(value: number | null | undefined): string {
    return (value ?? 0).toLocaleString('en-US');
  }

  // ===== إدارة المواصفات =====
  protected openAddSpec(): void {
    this.editingSpecId.set(null);
    this.sName.set('');
    this.sValue.set('');
    this.sUnit.set('');
    this.showSpecForm.set(true);
  }

  protected openEditSpec(s: ProjectSpecification): void {
    this.editingSpecId.set(s.id);
    this.sName.set(s.specificationName);
    this.sValue.set(s.specificationValue);
    this.sUnit.set(s.unit);
    this.showSpecForm.set(true);
  }

  protected closeSpecForm(): void {
    this.showSpecForm.set(false);
  }

  protected saveSpec(): void {
    if (this.savingSpec()) return;
    if (!this.sName().trim() || !this.sValue().trim() || !this.sUnit().trim()) {
      return;
    }
    this.savingSpec.set(true);
    const dto = {
      specificationName: this.sName().trim(),
      specificationValue: this.sValue().trim(),
      unit: this.sUnit().trim(),
    };
    const editId = this.editingSpecId();
    const req = editId
      ? this.specsService.update(this.subId, editId, dto)
      : this.specsService.create(this.subId, dto);

    req.subscribe({
      next: () => {
        this.savingSpec.set(false);
        this.showSpecForm.set(false);
        this.loadSpecs();
      },
      error: (err) => {
        this.savingSpec.set(false);
        alert(err?.error?.message ?? 'تعذّر حفظ المواصفة');
      },
    });
  }

  protected deleteSpec(s: ProjectSpecification): void {
    if (!confirm(`حذف المواصفة «${s.specificationName}»؟`)) {
      return;
    }
    this.specsService.remove(this.subId, s.id).subscribe({
      next: () => this.loadSpecs(),
      error: (err) => alert(err?.error?.message ?? 'تعذّر حذف المواصفة'),
    });
  }
}
