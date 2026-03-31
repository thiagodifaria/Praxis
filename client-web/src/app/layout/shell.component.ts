import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthStore } from '../core/auth.store';
import { Branch, FeatureFlag, NotificationItem } from '../core/app.models';
import { NAVIGATION_ITEMS } from '../core/navigation';
import { RealtimeService } from '../core/realtime.service';
import { branchScopedLabel, formatDateTime, formatStatus, notificationSeverityLabels, toneForStatus } from '../shared/ui.helpers';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly auth = inject(AuthStore);
  readonly realtime = inject(RealtimeService);

  readonly loading = signal(false);
  readonly currentUrl = signal(this.router.url);
  readonly branches = signal<Branch[]>([]);
  readonly featureFlags = signal<FeatureFlag[]>([]);
  readonly notifications = signal<NotificationItem[]>([]);
  readonly notificationDrawerOpen = signal(false);
  readonly selectedBranchId = signal<string | null>(this.auth.activeBranchId());

  readonly visibleNavigation = computed(() =>
    NAVIGATION_ITEMS.filter((item) => this.moduleEnabled(item.moduleKey)),
  );

  readonly unreadCount = computed(() => this.notifications().filter((item) => !item.isRead).length);
  readonly activeNavigation = computed(() => {
    const current = this.currentUrl();
    return this.visibleNavigation().find((item) => current === item.route || current.startsWith(`${item.route}/`))
      ?? this.visibleNavigation()[0]
      ?? null;
  });
  readonly activeNavigationLabel = computed(() => this.activeNavigation()?.label ?? 'Workspace');
  readonly activeNavigationDescription = computed(() => this.activeNavigation()?.description ?? 'Controle operacional centralizado');
  readonly activeBranchName = computed(() => {
    const branchId = this.selectedBranchId();
    if (!branchId) {
      return 'Rede inteira';
    }

    return this.branches().find((item) => item.id === branchId)?.name ?? 'Filial ativa';
  });
  readonly userInitials = computed(() =>
    this.auth.userName()
      .split(' ')
      .map((part) => part.trim())
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join(''),
  );
  readonly currentDateLabel = computed(() =>
    new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: 'long',
      year: 'numeric',
    }).format(new Date()),
  );

  readonly liveFeed = computed(() =>
    this.realtime.liveMessages().filter((message) => {
      const branchId = this.selectedBranchId();
      return !branchId || !message.branchId || message.branchId === branchId;
    }),
  );

  readonly formatDateTime = formatDateTime;
  readonly formatStatus = formatStatus;
  readonly toneForStatus = toneForStatus;
  readonly notificationSeverityLabels = notificationSeverityLabels;
  readonly branchScopedLabel = branchScopedLabel;

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.currentUrl.set(event.urlAfterRedirects);
      });

    effect(() => {
      const branchId = this.auth.activeBranchId();
      this.selectedBranchId.set(branchId);
      void this.loadWorkspace(branchId);
    });

    effect(() => {
      const latest = this.liveFeed()[0];
      if (!latest) {
        return;
      }

      const currentBranchId = this.selectedBranchId();
      if (currentBranchId && latest.branchId && latest.branchId !== currentBranchId) {
        return;
      }

      this.notifications.update((current) => [
        {
          id: `${latest.routingKey}-${latest.publishedAtUtc}`,
          routingKey: latest.routingKey,
          source: latest.source,
          title: latest.title,
          message: latest.message,
          severity: latest.severity,
          isRead: false,
          publishedAtUtc: latest.publishedAtUtc,
          branchId: latest.branchId,
          branchName: null,
          metadataJson: latest.metadataJson,
        },
        ...current,
      ].slice(0, 30));
    });
  }

  async logout(): Promise<void> {
    await this.realtime.disconnect();
    this.auth.logout();
  }

  async refreshWorkspace(): Promise<void> {
    await this.loadWorkspace(this.selectedBranchId());
  }

  async changeBranch(branchId: string | null): Promise<void> {
    this.auth.setActiveBranch(branchId || null);
    await this.router.navigate(['/dashboard']);
  }

  async markAsRead(id: string): Promise<void> {
    if (id.includes('-') && id.length < 40) {
      this.notifications.update((current) =>
        current.map((item) => (item.id === id ? { ...item, isRead: true } : item)),
      );
      return;
    }

    await firstValueFrom(this.api.markNotificationRead(id));
    this.notifications.update((current) =>
      current.map((item) => (item.id === id ? { ...item, isRead: true } : item)),
    );
  }

  async markAllRead(): Promise<void> {
    await firstValueFrom(this.api.markAllNotificationsRead(this.selectedBranchId()));
    this.notifications.update((current) => current.map((item) => ({ ...item, isRead: true })));
  }

  toggleNotificationDrawer(): void {
    this.notificationDrawerOpen.update((value) => !value);
  }

  moduleEnabled(moduleKey: string): boolean {
    const flags = this.featureFlags();
    if (!flags.length) {
      return true;
    }

    const branchId = this.selectedBranchId();
    const scoped = flags.find((flag) => flag.moduleKey === moduleKey && flag.branchId === branchId);
    if (scoped) {
      return scoped.isEnabled;
    }

    const global = flags.find((flag) => flag.moduleKey === moduleKey && !flag.branchId);
    return global?.isEnabled ?? true;
  }

  private async loadWorkspace(branchId?: string | null): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      return;
    }

    this.loading.set(true);

    try {
      const [branches, featureFlags, notifications] = await Promise.all([
        firstValueFrom(this.api.listBranches()),
        firstValueFrom(this.api.listFeatureFlags(branchId)),
        firstValueFrom(this.api.listNotifications(branchId, false, 30)),
      ]);

      this.branches.set(branches);
      this.featureFlags.set(featureFlags);
      this.notifications.set(notifications);

      if (!this.auth.activeBranchId() && branches.length) {
        const headquarters = branches.find((item) => item.isHeadquarters) ?? branches[0];
        this.auth.setActiveBranch(headquarters.id);
      }

      await this.realtime.ensureConnected();
    } finally {
      this.loading.set(false);
    }
  }
}
