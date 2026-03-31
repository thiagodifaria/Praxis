import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Category, Product, Supplier } from '../../core/app.models';
import { formatCurrency } from '../../shared/ui.helpers';

@Component({
  selector: 'app-catalog-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Catalog</p>
          <h1>Base mestre comercial</h1>
          <p class="page-subtitle">
            Cadastros essenciais do OMS: categorias, fornecedores e produtos com snapshot de custo e preco.
          </p>
        </div>
      </header>

      <div class="two-column-layout">
        <div class="stack">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Category</p>
                <h3>{{ categoryForm.id ? 'Editar categoria' : 'Nova categoria' }}</h3>
              </div>
            </div>

            <form class="stack" (ngSubmit)="saveCategory()">
              <div class="form-grid">
                <label class="field">
                  <span>Codigo</span>
                  <input [(ngModel)]="categoryForm.code" name="categoryCode" required />
                </label>
                <label class="field">
                  <span>Nome</span>
                  <input [(ngModel)]="categoryForm.name" name="categoryName" required />
                </label>
              </div>
              <label class="field">
                <span>Descricao</span>
                <textarea [(ngModel)]="categoryForm.description" name="categoryDescription"></textarea>
              </label>
              <label class="field">
                <span>Ativa</span>
                <select [(ngModel)]="categoryForm.isActive" name="categoryActive">
                  <option [ngValue]="true">Sim</option>
                  <option [ngValue]="false">Nao</option>
                </select>
              </label>
              <div class="form-actions">
                <button class="button" type="submit">Salvar categoria</button>
                <button class="button button-ghost" type="button" (click)="resetCategory()">Limpar</button>
              </div>
            </form>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Supplier</p>
                <h3>{{ supplierForm.id ? 'Editar fornecedor' : 'Novo fornecedor' }}</h3>
              </div>
            </div>

            <form class="stack" (ngSubmit)="saveSupplier()">
              <div class="form-grid">
                <label class="field">
                  <span>Codigo</span>
                  <input [(ngModel)]="supplierForm.code" name="supplierCode" required />
                </label>
                <label class="field">
                  <span>Nome</span>
                  <input [(ngModel)]="supplierForm.name" name="supplierName" required />
                </label>
                <label class="field">
                  <span>Contato</span>
                  <input [(ngModel)]="supplierForm.contactName" name="supplierContact" />
                </label>
                <label class="field">
                  <span>E-mail</span>
                  <input [(ngModel)]="supplierForm.email" name="supplierEmail" />
                </label>
                <label class="field">
                  <span>Telefone</span>
                  <input [(ngModel)]="supplierForm.phone" name="supplierPhone" />
                </label>
                <label class="field">
                  <span>Ativo</span>
                  <select [(ngModel)]="supplierForm.isActive" name="supplierActive">
                    <option [ngValue]="true">Sim</option>
                    <option [ngValue]="false">Nao</option>
                  </select>
                </label>
              </div>
              <div class="form-actions">
                <button class="button" type="submit">Salvar fornecedor</button>
                <button class="button button-ghost" type="button" (click)="resetSupplier()">Limpar</button>
              </div>
            </form>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Product</p>
                <h3>{{ productForm.id ? 'Editar produto' : 'Novo produto' }}</h3>
              </div>
            </div>

            <form class="stack" (ngSubmit)="saveProduct()">
              <div class="form-grid">
                <label class="field">
                  <span>SKU</span>
                  <input [(ngModel)]="productForm.sku" name="productSku" required />
                </label>
                <label class="field">
                  <span>Nome</span>
                  <input [(ngModel)]="productForm.name" name="productName" required />
                </label>
                <label class="field">
                  <span>Preco</span>
                  <input type="number" min="0" step="0.01" [(ngModel)]="productForm.unitPrice" name="productPrice" required />
                </label>
                <label class="field">
                  <span>Custo padrao</span>
                  <input type="number" min="0" step="0.01" [(ngModel)]="productForm.standardCost" name="productCost" required />
                </label>
                <label class="field">
                  <span>Reorder level</span>
                  <input type="number" min="0" [(ngModel)]="productForm.reorderLevel" name="productReorder" required />
                </label>
                <label class="field">
                  <span>Categoria</span>
                  <select [(ngModel)]="productForm.categoryId" name="productCategory" required>
                    <option value="">Selecione</option>
                    <option *ngFor="let category of categories()" [value]="category.id">{{ category.code }} · {{ category.name }}</option>
                  </select>
                </label>
                <label class="field">
                  <span>Fornecedor</span>
                  <select [(ngModel)]="productForm.supplierId" name="productSupplier">
                    <option value="">Opcional</option>
                    <option *ngFor="let supplier of suppliers()" [value]="supplier.id">{{ supplier.code }} · {{ supplier.name }}</option>
                  </select>
                </label>
                <label class="field">
                  <span>Ativo</span>
                  <select [(ngModel)]="productForm.isActive" name="productActive">
                    <option [ngValue]="true">Sim</option>
                    <option [ngValue]="false">Nao</option>
                  </select>
                </label>
              </div>
              <label class="field">
                <span>Descricao</span>
                <textarea [(ngModel)]="productForm.description" name="productDescription"></textarea>
              </label>
              <div class="form-actions">
                <button class="button" type="submit">Salvar produto</button>
                <button class="button button-ghost" type="button" (click)="resetProduct()">Limpar</button>
              </div>
            </form>
          </section>
        </div>

        <div class="stack">
          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Categories</p>
                <h3>Mapa de classificacao</h3>
              </div>
            </div>

            <div class="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>Codigo</th>
                    <th>Nome</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let category of categories()">
                    <td>{{ category.code }}</td>
                    <td>{{ category.name }}</td>
                    <td><span class="badge" [ngClass]="category.isActive ? 'tone-good' : 'tone-neutral'">{{ category.isActive ? 'Ativa' : 'Inativa' }}</span></td>
                    <td><button class="button button-ghost" type="button" (click)="editCategory(category)">Editar</button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Suppliers</p>
                <h3>Ecossistema de compra</h3>
              </div>
            </div>

            <div class="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>Codigo</th>
                    <th>Fornecedor</th>
                    <th>Contato</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let supplier of suppliers()">
                    <td>{{ supplier.code }}</td>
                    <td>{{ supplier.name }}</td>
                    <td>{{ supplier.contactName || supplier.email || '--' }}</td>
                    <td><button class="button button-ghost" type="button" (click)="editSupplier(supplier)">Editar</button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <section class="panel">
            <div class="section-title compact">
              <div>
                <p class="eyebrow">Products</p>
                <h3>Portifolio operacional</h3>
              </div>
            </div>

            <div class="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>Produto</th>
                    <th>Preco</th>
                    <th>Custo</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let product of products()">
                    <td>{{ product.sku }}</td>
                    <td>
                      {{ product.name }}
                      <small>{{ product.categoryName }}</small>
                    </td>
                    <td>{{ formatCurrency(product.unitPrice) }}</td>
                    <td>{{ formatCurrency(product.standardCost) }}</td>
                    <td><button class="button button-ghost" type="button" (click)="editProduct(product)">Editar</button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </div>
    </section>
  `,
})
export class CatalogPageComponent {
  private readonly api = inject(ApiService);

  readonly categories = signal<Category[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly products = signal<Product[]>([]);
  readonly formatCurrency = formatCurrency;

  categoryForm = { id: '', code: '', name: '', description: '', isActive: true };
  supplierForm = { id: '', code: '', name: '', contactName: '', email: '', phone: '', isActive: true };
  productForm = {
    id: '',
    sku: '',
    name: '',
    description: '',
    unitPrice: 0,
    standardCost: 0,
    reorderLevel: 0,
    categoryId: '',
    supplierId: '',
    isActive: true,
  };

  constructor() {
    void this.load();
  }

  async saveCategory(): Promise<void> {
    const payload = {
      code: this.categoryForm.code,
      name: this.categoryForm.name,
      description: this.categoryForm.description,
      isActive: this.categoryForm.isActive,
    };

    if (this.categoryForm.id) {
      await firstValueFrom(this.api.updateCategory(this.categoryForm.id, payload));
    } else {
      await firstValueFrom(this.api.createCategory(payload));
    }

    this.resetCategory();
    await this.load();
  }

  async saveSupplier(): Promise<void> {
    const payload = {
      code: this.supplierForm.code,
      name: this.supplierForm.name,
      contactName: this.supplierForm.contactName || null,
      email: this.supplierForm.email || null,
      phone: this.supplierForm.phone || null,
      isActive: this.supplierForm.isActive,
    };

    if (this.supplierForm.id) {
      await firstValueFrom(this.api.updateSupplier(this.supplierForm.id, payload));
    } else {
      await firstValueFrom(this.api.createSupplier(payload));
    }

    this.resetSupplier();
    await this.load();
  }

  async saveProduct(): Promise<void> {
    const payload = {
      sku: this.productForm.sku,
      name: this.productForm.name,
      description: this.productForm.description,
      unitPrice: this.productForm.unitPrice,
      standardCost: this.productForm.standardCost,
      reorderLevel: this.productForm.reorderLevel,
      categoryId: this.productForm.categoryId,
      supplierId: this.productForm.supplierId || null,
      isActive: this.productForm.isActive,
    };

    if (this.productForm.id) {
      await firstValueFrom(this.api.updateProduct(this.productForm.id, payload));
    } else {
      await firstValueFrom(this.api.createProduct(payload));
    }

    this.resetProduct();
    await this.load();
  }

  editCategory(category: Category): void {
    this.categoryForm = { ...category };
  }

  editSupplier(supplier: Supplier): void {
    this.supplierForm = {
      id: supplier.id,
      code: supplier.code,
      name: supplier.name,
      contactName: supplier.contactName ?? '',
      email: supplier.email ?? '',
      phone: supplier.phone ?? '',
      isActive: supplier.isActive,
    };
  }

  editProduct(product: Product): void {
    this.productForm = {
      id: product.id,
      sku: product.sku,
      name: product.name,
      description: product.description,
      unitPrice: product.unitPrice,
      standardCost: product.standardCost,
      reorderLevel: product.reorderLevel,
      categoryId: product.categoryId,
      supplierId: product.supplierId ?? '',
      isActive: product.isActive,
    };
  }

  resetCategory(): void {
    this.categoryForm = { id: '', code: '', name: '', description: '', isActive: true };
  }

  resetSupplier(): void {
    this.supplierForm = { id: '', code: '', name: '', contactName: '', email: '', phone: '', isActive: true };
  }

  resetProduct(): void {
    this.productForm = {
      id: '',
      sku: '',
      name: '',
      description: '',
      unitPrice: 0,
      standardCost: 0,
      reorderLevel: 0,
      categoryId: '',
      supplierId: '',
      isActive: true,
    };
  }

  private async load(): Promise<void> {
    const [categories, suppliers, products] = await Promise.all([
      firstValueFrom(this.api.listCategories()),
      firstValueFrom(this.api.listSuppliers()),
      firstValueFrom(this.api.listProducts()),
    ]);

    this.categories.set(categories);
    this.suppliers.set(suppliers);
    this.products.set(products);
  }
}
