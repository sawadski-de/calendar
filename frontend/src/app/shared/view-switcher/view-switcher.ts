import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type CalendarViewType = 'month' | 'week' | 'day';

interface ViewOption {
  value: CalendarViewType;
  labelKey: string;
}

@Component({
  selector: 'app-view-switcher',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './view-switcher.html',
  styleUrl: './view-switcher.css',
})
export class ViewSwitcher {
  readonly active = input.required<CalendarViewType>();
  readonly activeChange = output<CalendarViewType>();

  readonly options: ViewOption[] = [
    { value: 'month', labelKey: 'calendar.viewMonth' },
    { value: 'week', labelKey: 'calendar.viewWeek' },
    { value: 'day', labelKey: 'calendar.viewDay' },
  ];

  select(value: CalendarViewType): void {
    if (value !== this.active()) {
      this.activeChange.emit(value);
    }
  }
}
