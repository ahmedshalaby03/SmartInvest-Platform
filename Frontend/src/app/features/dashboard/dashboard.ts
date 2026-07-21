import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  template: `
    <div class="page">
      <header class="page-head">
        <div>
          <h1>لوحة التحكم</h1>
          <p>مرحبًا، {{ auth.user()?.fullName }} 👋</p>
        </div>
      </header>

      <div class="cards">
        <div class="kpi"><span class="lab">إجمالي المشروعات</span><b class="val">—</b></div>
        <div class="kpi ok"><span class="lab">المعتمدة</span><b class="val">—</b></div>
        <div class="kpi warn"><span class="lab">قيد المراجعة</span><b class="val">—</b></div>
        <div class="kpi gold"><span class="lab">إجمالي التمويل</span><b class="val">—</b></div>
      </div>

      <div class="placeholder">
        <div class="ph-icon">📊</div>
        <h3>لوحة تحكم مدير التخطيط</h3>
        <p>ملخص المؤشرات والإحصائيات — قيد الربط بالـ API.</p>
      </div>
    </div>
  `,
  styles: [
    `
    .page { padding: 24px 28px; }
    .page-head { margin-bottom: 22px; }
    .page-head h1 { font-size: 22px; }
    .page-head p { margin: 2px 0 0; color: var(--muted); font-size: 13px; }
    .cards { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 20px; }
    @media (max-width: 720px) { .cards { grid-template-columns: repeat(2, 1fr); } }
    .kpi { background: var(--surface); border: 1px solid var(--line); border-radius: var(--radius); padding: 16px; box-shadow: var(--shadow); position: relative; overflow: hidden; }
    .kpi::before { content: ""; position: absolute; inset-block: 0; inset-inline-start: 0; width: 4px; background: var(--green-600); }
    .kpi.ok::before { background: var(--ok); } .kpi.warn::before { background: var(--warn); } .kpi.gold::before { background: var(--gold); }
    .lab { color: var(--muted); font-size: 12px; font-weight: 700; }
    .val { display: block; font-size: 26px; font-weight: 800; margin-top: 4px; }
    .placeholder { background: var(--surface); border: 1px dashed var(--line-strong); border-radius: var(--radius); padding: 44px; text-align: center; box-shadow: var(--shadow); }
    .ph-icon { font-size: 44px; }
    .placeholder h3 { margin: 12px 0 6px; }
    .placeholder p { color: var(--muted); margin: 0; }
    `,
  ],
})
export class Dashboard {
  protected readonly auth = inject(AuthService);
}
