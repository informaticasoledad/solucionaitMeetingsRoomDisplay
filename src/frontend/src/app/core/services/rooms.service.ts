import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Room, CreateRoomRequest, UpdateRoomRequest } from '../models/room.model';
import { RoomStatus, QuickReserveRequest, LocalMeeting, CreateMeetingRequest } from '../models/room-status.model';
import { map, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RoomsService {
  private base = '/api';
  rooms = signal<Room[]>([]);
  allStatuses = signal<RoomStatus[]>([]);
  localMeetings = signal<LocalMeeting[]>([]);

  constructor(private http: HttpClient) {}

  loadRooms() {
    return this.http.get<Room[]>(`${this.base}/rooms`).pipe(
      map((rooms) => { this.rooms.set(rooms); return rooms; }),
    );
  }

  getRoom(id: string): Observable<Room> { return this.http.get<Room>(`${this.base}/rooms/${id}`); }
  createRoom(request: CreateRoomRequest): Observable<Room> { return this.http.post<Room>(`${this.base}/rooms`, request); }
  updateRoom(id: string, request: UpdateRoomRequest): Observable<Room> { return this.http.put<Room>(`${this.base}/rooms/${id}`, request); }
  deleteRoom(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/rooms/${id}`); }

  getAllStatuses(): Observable<RoomStatus[]> {
    return this.http.get<RoomStatus[]>(`${this.base}/status`).pipe(map(s => { this.allStatuses.set(s); return s; }));
  }

  getRoomStatus(id: string): Observable<RoomStatus> { return this.http.get<RoomStatus>(`${this.base}/status/${id}`); }
  quickReserve(request: QuickReserveRequest) { return this.http.post(`${this.base}/reservations/quick`, request); }
  setCredentials(credentialsJson: string, provider: string) { return this.http.post(`${this.base}/calendar/credentials`, { credentialsJson, provider }); }
  syncCalendars() { return this.http.post(`${this.base}/calendar/sync`, {}); }

  loadMeetings(roomId?: string) {
    const params = roomId ? `?roomId=${encodeURIComponent(roomId)}` : '';
    return this.http.get<LocalMeeting[]>(`${this.base}/meetings${params}`).pipe(map(m => { this.localMeetings.set(m); return m; }));
  }

  createMeeting(request: CreateMeetingRequest) { return this.http.post<LocalMeeting>(`${this.base}/meetings`, request); }
  deleteMeeting(id: string) { return this.http.delete<void>(`${this.base}/meetings/${id}`); }
}

