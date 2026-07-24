import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateFinancialYear, FinancialYear, UpdateFinancialYear } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class FinancialYearsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/financial-years`;

  getAll(): Observable<FinancialYear[]> {
    return this.http.get<FinancialYear[]>(this.base);
  }

  getById(id: number): Observable<FinancialYear> {
    return this.http.get<FinancialYear>(`${this.base}/${id}`);
  }

  create(dto: CreateFinancialYear): Observable<FinancialYear> {
    return this.http.post<FinancialYear>(this.base, dto);
  }

  update(id: number, dto: UpdateFinancialYear): Observable<FinancialYear> {
    return this.http.put<FinancialYear>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
