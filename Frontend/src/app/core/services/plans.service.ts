import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApprovePlan, CreatePlan, Plan, PlanDetail, UpdatePlan } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class PlansService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/plans`;

  getAll(): Observable<Plan[]> {
    return this.http.get<Plan[]>(this.base);
  }

  getById(id: number): Observable<PlanDetail> {
    return this.http.get<PlanDetail>(`${this.base}/${id}`);
  }

  create(dto: CreatePlan): Observable<Plan> {
    return this.http.post<Plan>(this.base, dto);
  }

  update(id: number, dto: UpdatePlan): Observable<Plan> {
    return this.http.put<Plan>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  addSuggestedProject(planId: number, subProjectId: number): Observable<PlanDetail> {
    return this.http.post<PlanDetail>(`${this.base}/${planId}/suggested-projects`, { subProjectId });
  }

  removeSuggestedProject(planId: number, subProjectId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planId}/suggested-projects/${subProjectId}`);
  }

  approve(planId: number, dto: ApprovePlan): Observable<Plan> {
    return this.http.put<Plan>(`${this.base}/${planId}/approve`, dto);
  }
}
