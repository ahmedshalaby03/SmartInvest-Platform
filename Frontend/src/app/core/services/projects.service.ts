import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMainProject,
  CreateSubProject,
  MainProjectDetail,
  MainProjectListItem,
  PagedResult,
  SubProjectListItem,
  SubProjectDetail,
  SubProjectFinancialYear,
  SubProjectSearchParams,
  UpdateMainProject,
  UpdateSubProject,
} from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // ===== المشاريع الرئيسية =====
  getMainProjects(): Observable<MainProjectListItem[]> {
    return this.http.get<MainProjectListItem[]>(`${this.base}/mainprojects`);
  }

  getMainProject(id: number): Observable<MainProjectDetail> {
    return this.http.get<MainProjectDetail>(`${this.base}/mainprojects/${id}`);
  }

  createMainProject(dto: CreateMainProject): Observable<MainProjectDetail> {
    return this.http.post<MainProjectDetail>(`${this.base}/mainprojects`, dto);
  }

  updateMainProject(id: number, dto: UpdateMainProject): Observable<MainProjectDetail> {
    return this.http.put<MainProjectDetail>(`${this.base}/mainprojects/${id}`, dto);
  }

  deleteMainProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/mainprojects/${id}`);
  }

  // ===== المشاريع الفرعية =====
  searchSubProjects(params: SubProjectSearchParams): Observable<PagedResult<SubProjectListItem>> {
    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    const optional: (keyof SubProjectSearchParams)[] = [
      'mainProjectId', 'mainProgramId', 'subProgramId', 'markazId',
      'priorityId', 'statusId', 'financialYearId', 'searchTerm',
    ];
    for (const key of optional) {
      const value = params[key];
      if (value != null && value !== '') {
        httpParams = httpParams.set(key, value as string | number);
      }
    }

    return this.http.get<PagedResult<SubProjectListItem>>(`${this.base}/subprojects`, {
      params: httpParams,
    });
  }

  getSubProject(id: number): Observable<SubProjectDetail> {
    return this.http.get<SubProjectDetail>(`${this.base}/subprojects/${id}`);
  }

  createSubProject(dto: CreateSubProject): Observable<SubProjectListItem> {
    return this.http.post<SubProjectListItem>(`${this.base}/subprojects`, dto);
  }

  updateSubProject(id: number, dto: UpdateSubProject): Observable<SubProjectListItem> {
    return this.http.put<SubProjectListItem>(`${this.base}/subprojects/${id}`, dto);
  }

  deleteSubProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/subprojects/${id}`);
  }

  // ===== السنوات المالية للمشروع الفرعي =====
  getSubProjectFinancialYears(subProjectId: number): Observable<SubProjectFinancialYear[]> {
    return this.http.get<SubProjectFinancialYear[]>(`${this.base}/subprojects/${subProjectId}/financial-years`);
  }

  linkFinancialYear(subProjectId: number, financialYearId: number): Observable<SubProjectFinancialYear> {
    return this.http.post<SubProjectFinancialYear>(
      `${this.base}/subprojects/${subProjectId}/financial-years`,
      { financialYearId },
    );
  }

  unlinkFinancialYear(subProjectId: number, financialYearId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/subprojects/${subProjectId}/financial-years/${financialYearId}`);
  }
}
