import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ChangeUserRolesRequest,
  CreateUserRequest,
  CreateUserResponse,
  ListUsersResponse,
  UserSummary,
} from './user-management.models';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  constructor(private readonly http: HttpClient) {}

  createUser(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>('/identity/users', request);
  }

  listUsers(query: string, page: number, pageSize: number): Observable<ListUsersResponse> {
    return this.http.get<ListUsersResponse>('/identity/users', { params: { query, page, pageSize } });
  }

  getUser(id: string): Observable<UserSummary> {
    return this.http.get<UserSummary>(`/identity/users/${id}`);
  }

  changeUserRoles(id: string, request: ChangeUserRolesRequest): Observable<UserSummary> {
    return this.http.put<UserSummary>(`/identity/users/${id}/roles`, request);
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.post<void>(`/identity/users/${id}/deactivate`, null);
  }
}
