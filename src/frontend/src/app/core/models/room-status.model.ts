export interface MeetingEvent {
  id: string;
  summary: string;
  organizer: string;
  start: string;
  end: string;
  isAllDay: boolean;
}

export interface RoomStatus {
  roomId: string;
  roomName: string;
  capacity: number;
  status: 'Free' | 'BusySoon' | 'Occupied';
  clockMode: string;
  currentMeeting: MeetingEvent | null;
  nextMeeting: MeetingEvent | null;
  nextAvailableAt: string | null;
  todaysEvents: MeetingEvent[];
}

export interface QuickReserveRequest {
  roomId: string;
  durationMinutes: number;
  title: string;
  organizerName: string;
  startTime: string;
}

export interface LocalMeeting {
  id: string;
  roomId: string;
  title: string;
  organizer: string;
  start: string;
  end: string;
}

export interface CreateMeetingRequest {
  roomId: string;
  title: string;
  organizer: string;
  durationMinutes: number;
  start: string;
}
