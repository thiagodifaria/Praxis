import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ApprovalRule, Branch, CostCenter, FeatureFlag } from '../../core/app.models';
import { approvalModuleOptions, branchScopedLabel, formatCurrency, toneForStatus } from '../../shared/ui.helpers';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Settings</p>
          <h1>Configuracoes estruturais</h1>
          <p class="page-subtitle">
            Filiais, centros de custo, feature flags e regras de aprovacao para governar a V3 do Praxis.
          </p>
        </div>
      </header>

      <div class="grid cols-2">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Branch</p>
              <h3>{{ branchForm.id ? 'Editar filial' : 'Nova filial' }}</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="saveBranch()">
            <div class="form-grid">
              <label class="field"><span>Codigo</span><input [(ngModel)]="branchForm.code" name="branchCode" required /></label>
              <label class="field"><span>Nome</span><input [(ngModel)]="branchForm.name" name="branchName" required /></label>
              <label class="field"><span>Razao social</span><input [(ngModel)]="branchForm.legalName" name="branchLegalName" required /></label>
              <label class="field"><span>Documento</span><input [(ngModel)]="branchForm.document" name="branchDocument" required /></label>
              <label class="field"><span>Cidade</span><input [(ngModel)]="branchForm.city" name="branchCity" required /></label>
              <label class="field"><span>UF</span><input [(ngModel)]="branchForm.state" name="branchState" required /></label>
              <label class="field"><span>Matriz</span><select [(ngModel)]="branchForm.isHeadquarters" name="branchHq"><option [ngValue]="true">Sim</option><option [ngValue]="false">Nao</option></select></label>
              <label class="field"><span>Ativa</span><select [(ngModel)]="branchForm.isActive" name="branchActive"><option [ngValue]="true">Sim</option><option [ngValue]="false">Nao</option></select></label>
            </div>
            <div class="form-actions">
              <button class="button" type="submit">Salvar filial</button>
              <button class="button button-ghost" type="button" (click)="resetBranch()">Limpar</button>
            </div>
          </form>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Cost Center</p>
              <h3>{{ costCenterForm.id ? 'Editar centro' : 'Novo centro' }}</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="saveCostCenter()">
            <div class="form-grid">
              <label class="field">
                <span>Filial</span>
                <select [(ngModel)]="costCenterForm.branchId" name="costCenterBranch" required>
                  <option value="">Selecione</option>
                  <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.code }} · {{ branch.name }}</option>
                </select>
              </label>
              <label class="field"><span>Codigo</span><input [(ngModel)]="costCenterForm.code" name="costCenterCode" required /></label>
              <label class="field"><span>Nome</span><input [(ngModel)]="costCenterForm.name" name="costCenterName" required /></label>
              <label class="field"><span>Ativo</span><select [(ngModel)]="costCenterForm.isActive" name="costCenterActive"><option [ngValue]="true">Sim</option><option [ngValue]="false">Nao</option></select></label>
            </div>
            <label class="field"><span>Descricao</span><textarea [(ngModel)]="costCenterForm.description" name="costCenterDescription"></textarea></label>
            <div class="form-actions">
              <button class="button" type="submit">Salvar centro</button>
              <button class="button button-ghost" type="button" (click)="resetCostCenter()">Limpar</button>
            </div>
          </form>
        </section>
      </div>

      <div class="grid cols-2">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Feature Flags</p>
              <h3>Modulos habilitados</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let flag of featureFlags()">
              <div class="toolbar">
                <strong>{{ flag.displayName }}</strong>
                <span class="badge" [ngClass]="flag.isEnabled ? 'tone-good' : 'tone-neutral'">
                  {{ flag.isEnabled ? 'Ativo' : 'Desligado' }}
                </span>
              </div>
              <p>{{ flag.description }}</p>
              <small>{{ branchScopedLabel(flag.branchName) }}</small>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="toggleFlag(flag)">
                  {{ flag.isEnabled ? 'Desabilitar' : 'Habilitar' }}
                </button>
              </div>
            </article>
          </div>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Approval Rules</p>
              <h3>{{ approvalRuleForm.id ? 'Editar regra' : 'Nova regra' }}</h3>
            </div>
          </div>

          <form class="stack" (ngSubmit)="saveApprovalRule()">
            <div class="form-grid">
              <label class="field"><span>Nome</span><input [(ngModel)]="approvalRuleForm.name" name="approvalRuleName" required /></label>
              <label class="field">
                <span>Modulo</span>
                <select [(ngModel)]="approvalRuleForm.module" name="approvalRuleModule">
                  <option *ngFor="let option of approvalModuleOptions" [ngValue]="option.value">{{ option.label }}</option>
                </select>
              </label>
              <label class="field">
                <span>Filial</span>
                <select [(ngModel)]="approvalRuleForm.branchId" name="approvalRuleBranch">
                  <option value="">Global</option>
                  <option *ngFor="let branch of branches()" [value]="branch.id">{{ branch.code }} · {{ branch.name }}</option>
                </select>
              </label>
              <label class="field"><span>Valor minimo</span><input type="number" min="0" step="0.01" [(ngModel)]="approvalRuleForm.minimumAmount" name="approvalRuleAmount" required /></label>
              <label class="field"><span>Role requerida</span><input [(ngModel)]="approvalRuleForm.requiredRoleName" name="approvalRuleRole" required /></label>
              <label class="field"><span>Ativa</span><select [(ngModel)]="approvalRuleForm.isActive" name="approvalRuleActive"><option [ngValue]="true">Sim</option><option [ngValue]="false">Nao</option></select></label>
            </div>
            <label class="field"><span>Descricao</span><textarea [(ngModel)]="approvalRuleForm.description" name="approvalRuleDescription"></textarea></label>
            <div class="form-actions">
              <button class="button" type="submit">Salvar regra</button>
              <button class="button button-ghost" type="button" (click)="resetApprovalRule()">Limpar</button>
            </div>
          </form>
        </section>
      </div>

      <div class="grid cols-2">
        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Branches</p>
              <h3>Mapa organizacional</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let branch of branches()">
              <div class="toolbar">
                <strong>{{ branch.code }} · {{ branch.name }}</strong>
                <span class="badge" [ngClass]="branch.isActive ? 'tone-good' : 'tone-neutral'">{{ branch.isActive ? 'Ativa' : 'Inativa' }}</span>
              </div>
              <p>{{ branch.city }}/{{ branch.state }} · {{ branch.document }}</p>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="editBranch(branch)">Editar</button>
              </div>
            </article>
          </div>
        </section>

        <section class="panel">
          <div class="section-title compact">
            <div>
              <p class="eyebrow">Cost Centers & Rules</p>
              <h3>Governanca operacional</h3>
            </div>
          </div>

          <div class="stack">
            <article class="list-card" *ngFor="let item of costCenters()">
              <strong>{{ item.code }} · {{ item.name }}</strong>
              <p>{{ item.branchName }} · {{ item.description }}</p>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="editCostCenter(item)">Editar</button>
              </div>
            </article>

            <article class="list-card" *ngFor="let item of approvalRules()">
              <div class="toolbar">
                <strong>{{ item.name }}</strong>
                <span class="badge" [ngClass]="item.isActive ? 'tone-good' : 'tone-neutral'">{{ item.isActive ? 'Ativa' : 'Inativa' }}</span>
              </div>
              <p>{{ formatCurrency(item.minimumAmount) }} · {{ item.requiredRoleName }} · {{ branchScopedLabel(item.branchName) }}</p>
              <div class="form-actions">
                <button class="button button-ghost" type="button" (click)="editApprovalRule(item)">Editar</button>
              </div>
            </article>
          </div>
        </section>
      </div>
    </section>
  `,
})
export class SettingsPageComponent {
  private readonly api = inject(ApiService);

  readonly branches = signal<Branch[]>([]);
  readonly costCenters = signal<CostCenter[]>([]);
  readonly featureFlags = signal<FeatureFlag[]>([]);
  readonly approvalRules = signal<ApprovalRule[]>([]);
  readonly approvalModuleOptions = approvalModuleOptions;
  readonly branchScopedLabel = branchScopedLabel;
  readonly formatCurrency = formatCurrency;
  readonly toneForStatus = toneForStatus;

  branchForm = {
    id: '',
    code: '',
    name: '',
    legalName: '',
    document: '',
    city: '',
    state: '',
    isHeadquarters: false,
    isActive: true,
  };

  costCenterForm = {
    id: '',
    branchId: '',
    code: '',
    name: '',
    description: '',
    isActive: true,
  };

  approvalRuleForm = {
    id: '',
    name: '',
    module: 0,
    branchId: '',
    minimumAmount: 0,
    requiredRoleName: '',
    description: '',
    isActive: true,
  };

  constructor() {
    effect(() => {
      void this.load();
    });
  }

  async saveBranch(): Promise<void> {
    const payload = { ...this.branchForm, id: undefined };
    if (this.branchForm.id) {
      await firstValueFrom(this.api.updateBranch(this.branchForm.id, payload));
    } else {
      await firstValueFrom(this.api.createBranch(payload));
    }

    this.resetBranch();
    await this.load();
  }

  async saveCostCenter(): Promise<void> {
    const payload = {
      branchId: this.costCenterForm.branchId,
      code: this.costCenterForm.code,
      name: this.costCenterForm.name,
      description: this.costCenterForm.description,
      isActive: this.costCenterForm.isActive,
    };

    if (this.costCenterForm.id) {
      await firstValueFrom(this.api.updateCostCenter(this.costCenterForm.id, payload));
    } else {
      await firstValueFrom(this.api.createCostCenter(payload));
    }

    this.resetCostCenter();
    await this.load();
  }

  async saveApprovalRule(): Promise<void> {
    const payload = {
      name: this.approvalRuleForm.name,
      module: this.approvalRuleForm.module,
      branchId: this.approvalRuleForm.branchId || null,
      minimumAmount: this.approvalRuleForm.minimumAmount,
      requiredRoleName: this.approvalRuleForm.requiredRoleName,
      description: this.approvalRuleForm.description,
      isActive: this.approvalRuleForm.isActive,
    };

    if (this.approvalRuleForm.id) {
      await firstValueFrom(this.api.updateApprovalRule(this.approvalRuleForm.id, payload));
    } else {
      await firstValueFrom(this.api.createApprovalRule(payload));
    }

    this.resetApprovalRule();
    await this.load();
  }

  async toggleFlag(flag: FeatureFlag): Promise<void> {
    await firstValueFrom(this.api.updateFeatureFlag(flag.id, {
      displayName: flag.displayName,
      description: flag.description,
      isEnabled: !flag.isEnabled,
    }));

    await this.load();
  }

  editBranch(branch: Branch): void {
    this.branchForm = { ...branch };
  }

  editCostCenter(costCenter: CostCenter): void {
    this.costCenterForm = {
      id: costCenter.id,
      branchId: costCenter.branchId,
      code: costCenter.code,
      name: costCenter.name,
      description: costCenter.description,
      isActive: costCenter.isActive,
    };
  }

  editApprovalRule(rule: ApprovalRule): void {
    this.approvalRuleForm = {
      id: rule.id,
      name: rule.name,
      module: Number(rule.module),
      branchId: rule.branchId ?? '',
      minimumAmount: rule.minimumAmount,
      requiredRoleName: rule.requiredRoleName,
      description: rule.description,
      isActive: rule.isActive,
    };
  }

  resetBranch(): void {
    this.branchForm = {
      id: '',
      code: '',
      name: '',
      legalName: '',
      document: '',
      city: '',
      state: '',
      isHeadquarters: false,
      isActive: true,
    };
  }

  resetCostCenter(): void {
    this.costCenterForm = {
      id: '',
      branchId: '',
      code: '',
      name: '',
      description: '',
      isActive: true,
    };
  }

  resetApprovalRule(): void {
    this.approvalRuleForm = {
      id: '',
      name: '',
      module: 0,
      branchId: '',
      minimumAmount: 0,
      requiredRoleName: '',
      description: '',
      isActive: true,
    };
  }

  private async load(): Promise<void> {
    const [branches, costCenters, featureFlags, approvalRules] = await Promise.all([
      firstValueFrom(this.api.listBranches()),
      firstValueFrom(this.api.listCostCenters()),
      firstValueFrom(this.api.listFeatureFlags()),
      firstValueFrom(this.api.listApprovalRules()),
    ]);

    this.branches.set(branches);
    this.costCenters.set(costCenters);
    this.featureFlags.set(featureFlags);
    this.approvalRules.set(approvalRules);
  }
}
