import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TableData } from './chart-data';

let nextId = 0;

/**
 * Accessible chart container: a titled <figure> with a plain-language summary, the chart
 * itself (projected, hidden from assistive tech) and a data-table twin.
 */
@Component({
  selector: 'app-chart-frame',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure class="chart" [attr.aria-labelledby]="titleId">
      <figcaption class="chart__caption">
        <h3 class="chart__title" [id]="titleId">{{ heading() }}</h3>
        <p class="chart__summary">{{ summary() }}</p>
      </figcaption>
      @if (empty()) {
        <p class="chart__empty">{{ emptyText() }}</p>
      } @else {
        <div class="chart__plot" aria-hidden="true" [style.height.px]="height()">
          <ng-content />
        </div>
        @if (table(); as data) {
          <details class="chart__table">
            <summary>Show data table</summary>
            <div class="table-scroll">
              <table class="data-table">
                <caption class="visually-hidden">
                  {{
                    heading()
                  }}
                </caption>
                <thead>
                  <tr>
                    @for (column of data.columns; track column) {
                      <th scope="col">{{ column }}</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (row of data.rows; track $index) {
                    <tr>
                      @for (cell of row; track $index) {
                        <td>{{ cell }}</td>
                      }
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </details>
        }
      }
    </figure>
  `,
  styleUrl: './chart-frame.scss',
})
export class ChartFrame {
  readonly heading = input.required<string>();
  readonly summary = input.required<string>();
  readonly table = input<TableData | null>(null);
  readonly empty = input(false);
  readonly emptyText = input('Nothing to show yet.');
  readonly height = input(260);

  protected readonly titleId = `chart-title-${nextId++}`;
}
