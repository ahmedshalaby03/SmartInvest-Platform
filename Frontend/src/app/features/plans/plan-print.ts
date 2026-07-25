import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PlansService } from '../../core/services/plans.service';
import { PlanDetail } from '../../core/models/project.models';

@Component({
  selector: 'app-plan-print',
  imports: [RouterLink, DatePipe],
  templateUrl: './plan-print.html',
  styleUrl: './plan-print.css',
})
export class PlanPrint {
  private readonly route = inject(ActivatedRoute);
  private readonly plansService = inject(PlansService);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly plan = signal<PlanDetail | null>(null);

  protected readonly totalBank = computed(
    () => this.plan()?.suggestedProjects.reduce((a, p) => a + p.bankFunding, 0) ?? 0,
  );
  protected readonly totalSelf = computed(
    () => this.plan()?.suggestedProjects.reduce((a, p) => a + p.selfFunding, 0) ?? 0,
  );
  protected readonly totalCost = computed(
    () => this.plan()?.suggestedProjects.reduce((a, p) => a + p.totalCost, 0) ?? 0,
  );

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.plansService.getById(id).subscribe({
      next: (p) => {
        this.plan.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل الخطة');
        this.loading.set(false);
      },
    });
  }

  protected money(value: number): string {
    return (value ?? 0).toLocaleString('en-US');
  }

  protected print(): void {
    window.print();
  }
}
