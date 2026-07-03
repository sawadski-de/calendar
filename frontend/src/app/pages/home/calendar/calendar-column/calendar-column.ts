import { Component, computed, input } from '@angular/core';
import { Appointment } from '../appointment.model';
import { addDays, startOfDay } from '../date-utils';
import { computeOverlapLayout, OverlapLayoutItem } from '../overlap-layout';

export const HOUR_HEIGHT_PX = 48;

interface PositionedAppointment {
  layout: OverlapLayoutItem;
  topPercent: number;
  heightPercent: number;
  leftPercent: number;
  widthPercent: number;
  timeLabel: string;
}

@Component({
  selector: 'app-calendar-column',
  standalone: true,
  templateUrl: './calendar-column.html',
  styleUrl: './calendar-column.css',
})
export class CalendarColumn {
  readonly date = input.required<Date>();
  readonly appointments = input<Appointment[]>([]);

  readonly totalHeightPx = 24 * HOUR_HEIGHT_PX;

  readonly positioned = computed<PositionedAppointment[]>(() => {
    const dayStart = startOfDay(this.date());
    const dayEnd = addDays(dayStart, 1);

    return computeOverlapLayout(this.appointments()).map((layout) => {
      const start = new Date(layout.appointment.startUtc);
      const end = new Date(layout.appointment.endUtc);

      // Clip to this day's 24h span — an appointment spanning midnight only occupies its portion here.
      const clippedStartMs = Math.max(start.getTime(), dayStart.getTime());
      const clippedEndMs = Math.min(end.getTime(), dayEnd.getTime());
      const startFractionOfDay = (clippedStartMs - dayStart.getTime()) / (24 * 60 * 60 * 1000);
      const endFractionOfDay = (clippedEndMs - dayStart.getTime()) / (24 * 60 * 60 * 1000);

      return {
        layout,
        topPercent: startFractionOfDay * 100,
        heightPercent: Math.max(endFractionOfDay - startFractionOfDay, 0) * 100,
        leftPercent: (layout.columnIndex / layout.columnCount) * 100,
        widthPercent: 100 / layout.columnCount,
        timeLabel: `${formatTime(start)}–${formatTime(end)}`,
      };
    });
  });
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}
