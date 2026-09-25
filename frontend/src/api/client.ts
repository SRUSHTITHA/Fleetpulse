import axios from 'axios';
import type {
  LoginRequest,
  LoginResponse,
  SignUpRequest,
  ResetPasswordRequest,
  GoogleLoginRequest,
  AuthProvidersResponse,
  UpdateUserRoleRequest,
  TripDto,
  UserDto,
  StartTripRequest,
  ReportLocationRequest,
  ReportLocationResponse,
  ExceptionRuleDto,
  CreateExceptionRuleRequest,
  UpdateExceptionRuleRequest,
  ExceptionAlertDto,
  VehicleDto,
  VehicleInput,
  DriverDto,
  GeocodeResultDto,
  RouteResultDto,
} from '../types';

const api = axios.create({
  baseURL: '/api',
});

export function setAuthToken(token: string | null) {
  if (token) {
    api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  } else {
    delete api.defaults.headers.common['Authorization'];
  }
}

export async function login(req: LoginRequest): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/auth/login', req);
  return data;
}

export async function getMe(token: string): Promise<UserDto> {
  const { data } = await api.get<UserDto>('/auth/me', {
    headers: { Authorization: `Bearer ${token}` },
  });
  return data;
}

export async function getTrips(): Promise<TripDto[]> {
  const { data } = await api.get<TripDto[]>('/trips');
  return data;
}

export async function startTrip(req: StartTripRequest): Promise<TripDto> {
  const { data } = await api.post<TripDto>('/trips/start', req);
  return data;
}

export async function endTrip(tripId: string): Promise<TripDto> {
  const { data } = await api.post<TripDto>('/trips/end', { tripId });
  return data;
}

export async function cancelTrip(tripId: string): Promise<TripDto> {
  const { data } = await api.post<TripDto>('/trips/cancel', { tripId });
  return data;
}

export async function getActiveTrip(): Promise<TripDto | null> {
  try {
    const { data } = await api.get<TripDto>('/trips/active');
    return data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}

export async function reportLocation(req: ReportLocationRequest): Promise<ReportLocationResponse> {
  const { data } = await api.post<ReportLocationResponse>('/location', req);
  return data;
}

export async function getExceptionRules(): Promise<ExceptionRuleDto[]> {
  const { data } = await api.get<ExceptionRuleDto[]>('/exception-rules');
  return data;
}

export async function getExceptions(): Promise<ExceptionAlertDto[]> {
  const { data } = await api.get<ExceptionAlertDto[]>('/exceptions');
  return data;
}

export async function deleteException(id: string): Promise<void> {
  await api.delete(`/exceptions/${id}`);
}

export async function createExceptionRule(
  req: CreateExceptionRuleRequest,
): Promise<ExceptionRuleDto> {
  const { data } = await api.post<ExceptionRuleDto>('/exception-rules', req);
  return data;
}

export async function updateExceptionRule(
  id: string,
  req: UpdateExceptionRuleRequest,
): Promise<ExceptionRuleDto> {
  const { data } = await api.put<ExceptionRuleDto>(`/exception-rules/${id}`, req);
  return data;
}

export async function deleteExceptionRule(id: string): Promise<void> {
  await api.delete(`/exception-rules/${id}`);
}

export async function getAuthProviders(): Promise<AuthProvidersResponse> {
  const { data } = await api.get<AuthProvidersResponse>('/auth/providers');
  return data;
}

export async function googleLogin(req: GoogleLoginRequest): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/auth/google', req);
  return data;
}

export async function signUp(req: SignUpRequest): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/auth/signup', req);
  return data;
}

export async function requestPasswordReset(email: string): Promise<{ message: string }> {
  const { data } = await api.post<{ message: string }>('/auth/forgot-password', { email });
  return data;
}

export async function resetPassword(req: ResetPasswordRequest): Promise<{ message: string }> {
  const { data } = await api.post<{ message: string }>('/auth/reset-password', req);
  return data;
}

export async function getVehicles(): Promise<VehicleDto[]> {
  const { data } = await api.get<VehicleDto[]>('/vehicles');
  return data;
}

export async function createVehicle(req: VehicleInput): Promise<VehicleDto> {
  const { data } = await api.post<VehicleDto>('/vehicles', req);
  return data;
}

export async function updateVehicle(id: string, req: VehicleInput): Promise<VehicleDto> {
  const { data } = await api.put<VehicleDto>(`/vehicles/${id}`, req);
  return data;
}

export async function deleteVehicle(id: string): Promise<void> {
  await api.delete(`/vehicles/${id}`);
}

export async function getDrivers(): Promise<DriverDto[]> {
  const { data } = await api.get<DriverDto[]>('/vehicles/drivers');
  return data;
}

export async function getUsers(): Promise<UserDto[]> {
  const { data } = await api.get<UserDto[]>('/auth/users');
  return data;
}

export async function updateUserRole(id: string, req: UpdateUserRoleRequest): Promise<UserDto> {
  const { data } = await api.put<UserDto>(`/auth/users/${id}/role`, req);
  return data;
}

export async function geocode(query: string): Promise<{ lat: number; lng: number; label?: string } | null> {
  const trimmed = query.trim();
  if (!trimmed) return null;
  try {
    const { data } = await api.get<GeocodeResultDto>('/geo/search', { params: { q: trimmed } });
    return { lat: data.latitude, lng: data.longitude, label: data.displayName };
  } catch {
    return null;
  }
}

export async function getDrivingRoute(
  origin: { lat: number; lng: number },
  dest: { lat: number; lng: number },
): Promise<[number, number][]> {
  const { data } = await api.get<RouteResultDto>('/geo/route', {
    params: {
      originLat: origin.lat,
      originLng: origin.lng,
      destLat: dest.lat,
      destLng: dest.lng,
    },
  });
  return (data.points ?? []).map((p) => [p.latitude, p.longitude]);
}
