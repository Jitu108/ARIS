import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateUserRequest, CreateUserResponse, ListUsersResponse } from './user-management.models';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  constructor(private readonly http: HttpClient) {}

  createUser(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>('/identity/users', request);
  }

  listUsers(query: string, page: number, pageSize: number): Observable<ListUsersResponse> {
    return this.http.get<ListUsersResponse>('/identity/users', { params: { query, page, pageSize } });
  }
}
