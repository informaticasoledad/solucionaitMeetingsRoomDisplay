export interface Room {
  id: string;
  name: string;
  capacity: number;
  calendarId: string;
  provider: string;
  clockMode: string;
}

export interface CreateRoomRequest {
  id: string;
  name: string;
  capacity: number;
  calendarId: string;
  provider: string;
  clockMode: string;
}

export interface UpdateRoomRequest {
  name: string;
  capacity: number;
  calendarId: string;
  provider: string;
  clockMode: string;
}
