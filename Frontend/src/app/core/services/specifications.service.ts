import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProjectSpecification, UpsertSpecification } from '../models/specification.models';

@Injectable({ providedIn: 'root' })
export class SpecificationsService {
  private readonly http = inject(HttpClient);
  private base(subProjectId: number): string {
    return `${environment.apiUrl}/subprojects/${subProjectId}/specifications`;
  }

  getAll(subProjectId: number): Observable<ProjectSpecification[]> {
    return this.http.get<ProjectSpecification[]>(this.base(subProjectId));
  }

  create(subProjectId: number, dto: UpsertSpecification): Observable<ProjectSpecification> {
    return this.http.post<ProjectSpecification>(this.base(subProjectId), dto);
  }

  update(subProjectId: number, id: number, dto: UpsertSpecification): Observable<ProjectSpecification> {
    return this.http.put<ProjectSpecification>(`${this.base(subProjectId)}/${id}`, dto);
  }

  remove(subProjectId: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.base(subProjectId)}/${id}`);
  }
}
