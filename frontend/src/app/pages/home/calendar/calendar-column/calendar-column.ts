import { Component, computed, input, output } from '@angular/core';
import { Activatable } from '../../../../shared/activatable/activatable';
import { StatusBadge } from '../status-badge/status-badge';
import { Appointment } from '../appointment.model';
import { addDays, formatTime, startOfDay } from '../date-utils';
import { computeOverlapLayout, OverlapLayoutItem } from '../overlap-layout';

export const HOUR_HEIGHT_PX = 48;
const MINUTES_PER_SLOT = 30;
const SLOT_COUNT = (24 * 60) / MINUTES_PER_SLOT;

interface PositionedAppointment {
  layout: OverlapLayoutItem;
  topPercent: number;
  heightPercent: number;
  leftPercent: number;
  widthPercent: number;
  timeLabel: string;
}

interface TimeSlot {
  index: number;
  start: Date;
  topPercent: number;
  heightPercent: number;
  occupied: boolean;
}

@Component({
  selector: 'app-calendar-column',
  standalone: true,
  imports: [StatusBadge, Activatable],
  templateUrl: './calendar-column.html',
  styleUrl: './calendar-column.css',
})
export class CalendarColumn {
  readonly date = input.required<Date>();
  readonly appointments = input<Appointment[]>([]);
  readonly slotActivated = output<Date>();
  readonly appointmentActivated = output<string>();

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

  // Half-hour slot grid for the create-appointment entry point (Story 1.3, AC 1). Reuses
  // `positioned()`'s already-clipped occupancy data instead of a second, separately-maintained
  // overlap calculation.
  readonly slots = computed<TimeSlot[]>(() => {
    const dayStart = startOfDay(this.date());
    const slotHeightPercent = 100 / SLOT_COUNT;
    const occupied = new Array(SLOT_COUNT).fill(false);

    for (const block of this.positioned()) {
      const startSlot = Math.max(0, Math.floor(block.topPercent / slotHeightPercent));
      const endSlot = Math.min(SLOT_COUNT, Math.ceil((block.topPercent + block.heightPercent) / slotHeightPercent));
      for (let i = startSlot; i < endSlot; i++) {
        occupied[i] = true;
      }
    }

    return Array.from({ length: SLOT_COUNT }, (_, index) => ({
      index,
      start: new Date(dayStart.getTime() + index * MINUTES_PER_SLOT * 60_000),
      topPercent: index * slotHeightPercent,
      heightPercent: slotHeightPercent,
      occupied: occupied[index],
    }));
  });

  activateSlot(slot: TimeSlot): void {
    if (!slot.occupied) {
      this.slotActivated.emit(slot.start);
    }
  }

  activateAppointment(appointmentId: string): void {
    this.appointmentActivated.emit(appointmentId);
  }
}
