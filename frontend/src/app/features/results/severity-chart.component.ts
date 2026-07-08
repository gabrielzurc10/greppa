import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  viewChild,
} from '@angular/core';
import { inject } from '@angular/core';
import { BarController, BarElement, CategoryScale, Chart, LinearScale, Tooltip } from 'chart.js';
import { ScanSummary, SEVERITY_ORDER, Severity } from '../../core/models';
import { ThemeService } from '../../core/theme.service';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip);

const SEVERITY_LABELS: Record<Severity, string> = {
  critical: 'Critical',
  high: 'High',
  medium: 'Medium',
  low: 'Low',
  info: 'Info',
};

/**
 * Status palette per theme — severity is a state, not a series;
 * the axis labels carry identity, color only reinforces it.
 */
const SEVERITY_COLORS: Record<Severity, { light: string; dark: string }> = {
  critical: { light: '#d03b3b', dark: '#d03b3b' },
  high: { light: '#ec835a', dark: '#ec835a' },
  medium: { light: '#fab219', dark: '#fab219' },
  low: { light: '#2a78d6', dark: '#4da3ff' },
  info: { light: '#898781', dark: '#7d8794' },
};

const MONO_FONT =
  "'JetBrains Mono', ui-monospace, 'SF Mono', 'Cascadia Code', Menlo, Consolas, monospace";

@Component({
  selector: 'app-severity-chart',
  template: `
    <div class="chart-wrap">
      <canvas #canvas role="img" aria-label="Findings by severity"></canvas>
    </div>
  `,
  styles: `
    .chart-wrap {
      position: relative;
      height: 220px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeverityChartComponent implements AfterViewInit, OnDestroy {
  readonly summary = input.required<ScanSummary>();

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly theme = inject(ThemeService);
  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      this.summary();
      this.theme.resolved();
      if (this.chart) {
        this.render();
      }
    });
  }

  ngAfterViewInit(): void {
    this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(): void {
    const mode = this.theme.resolved();
    const styles = getComputedStyle(document.documentElement);
    const ink = styles.getPropertyValue('--text-primary').trim();
    const muted = styles.getPropertyValue('--text-muted').trim();
    const grid = styles.getPropertyValue('--border').trim();
    const surface = styles.getPropertyValue('--surface-accent').trim();
    const summary = this.summary();
    const counts = SEVERITY_ORDER.map((s) => summary[s]);

    this.chart?.destroy();
    this.chart = new Chart(this.canvas().nativeElement, {
      type: 'bar',
      data: {
        labels: SEVERITY_ORDER.map((s) => SEVERITY_LABELS[s]),
        datasets: [
          {
            data: counts,
            backgroundColor: SEVERITY_ORDER.map((s) => SEVERITY_COLORS[s][mode]),
            borderRadius: 4,
            barThickness: 18,
          },
        ],
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        layout: { padding: { right: 32 } },
        scales: {
          x: {
            beginAtZero: true,
            ticks: { precision: 0, color: muted, font: { family: MONO_FONT, size: 11 } },
            grid: { color: grid },
            border: { display: false },
          },
          y: {
            ticks: { color: ink, font: { family: MONO_FONT, size: 12, weight: 600 } },
            grid: { display: false },
            border: { display: false },
          },
        },
        plugins: {
          tooltip: {
            backgroundColor: surface,
            borderColor: styles.getPropertyValue('--border-strong').trim(),
            borderWidth: 1,
            titleColor: ink,
            bodyColor: ink,
            titleFont: { family: MONO_FONT },
            bodyFont: { family: MONO_FONT },
            displayColors: false,
            callbacks: {
              label: (ctx) => ` ${ctx.parsed.x} finding${ctx.parsed.x === 1 ? '' : 's'}`,
            },
          },
        },
      },
      plugins: [
        {
          // Direct value labels at each bar end, in ink (never the series color).
          id: 'countLabels',
          afterDatasetsDraw: (chart) => {
            const { ctx } = chart;
            const meta = chart.getDatasetMeta(0);
            ctx.save();
            ctx.fillStyle = ink;
            ctx.font = `600 12px ${MONO_FONT}`;
            ctx.textBaseline = 'middle';
            meta.data.forEach((bar, i) => {
              ctx.fillText(String(counts[i]), bar.x + 8, bar.y);
            });
            ctx.restore();
          },
        },
      ],
    });
  }
}
