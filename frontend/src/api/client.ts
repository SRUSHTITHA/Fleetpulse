import axios, { type AxiosRequestConfig } from 'axios';
import { isApiErrorStatus } from '../utils/helpers';
import type { AuthProvidersResponse, CreateExceptionRuleRequest, DriverDto, ExceptionAlertDto, ExceptionRuleDto, GeocodeResultDto,
  GoogleLoginRequest, LoginRequest, LoginResponse, MessageResponse, ReportLocationRequest, ReportLocationResponse,
  ResetPasswordRequest, RouteResultDto, SignUpRequest, StartTripRequest, TripDto,
  UpdateExceptionRuleRequest, UpdateUserRoleRequest, UserDto, VehicleDto, VehicleInput,
} from '../types';

const api = axios.create({ baseURL: '/api' });

async function request<T>(config: AxiosRequestConfig): Promise<T> {
  const { data } = await api.request<T>(config);
  return data;
}

export function setAuthToken(token: string | null) {
  if (token) {
    api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  } else {
    delete api.defaults.headers.common['Authorization'];
  }
}

export async function login(req: LoginRequest): Promise<LoginResponse> {
  return request<LoginResponse>({ method: 'POST', url: '/auth/login', data: req });
}

export async function getMe(token: string): Promise<UserDto> {
  return request<UserDto>({ method: 'GET', url: '/auth/me', headers: { Authorization: `Bearer ${token}` },
  });
}

export async function getTrips(): Promise<TripDto[]> {
  return request<TripDto[]>({ method: 'GET', url: '/trips' });
}

export async function startTrip(req: StartTripRequest): Promise<TripDto> {
  return request<TripDto>({ method: 'POST', url: '/trips/start', data: req });
}

export async function endTrip(tripId: string): Promise<TripDto> {
  return request<TripDto>({ method: 'POST', url: '/trips/end', data: { tripId } });
}

export async function cancelTrip(tripId: string): Promise<TripDto> {
  return request<TripDto>({ method: 'POST', url: '/trips/cancel', data: { tripId } });
}

export async function getActiveTrip(): Promise<TripDto | null> {
  try {
    return await request<TripDto>({ method: 'GET', url: '/trips/active' });
  } catch (error) {
    if (isApiErrorStatus(error, 404)) {
      return null;
    }
    throw error;
  }
}

export async function reportLocation(req: ReportLocationRequest): Promise<ReportLocationResponse> {
  return request<ReportLocationResponse>({ method: 'POST', url: '/location', data: req });
}

export async function getExceptionRules(): Promise<ExceptionRuleDto[]> {
  return request<ExceptionRuleDto[]>({ method: 'GET', url: '/exception-rules' });
}

export async function getExceptions(): Promise<ExceptionAlertDto[]> {
  return request<ExceptionAlertDto[]>({ method: 'GET', url: '/exceptions' });
}

export async function deleteException(id: string): Promise<void> {
  await request<void>({ method: 'DELETE', url: `/exceptions/${id}` });
}

export async function createExceptionRule(
  req: CreateExceptionRuleRequest,
): Promise<ExceptionRuleDto> {
  return request<ExceptionRuleDto>({ method: 'POST', url: '/exception-rules', data: req });
}

export async function updateExceptionRule( id: string, req: UpdateExceptionRuleRequest ): Promise<ExceptionRuleDto> {
  return request<ExceptionRuleDto>({ method: 'PUT', url: `/exception-rules/${id}`, data: req });
}

export async function deleteExceptionRule(id: string): Promise<void> {
  await request<void>({ method: 'DELETE', url: `/exception-rules/${id}` });
}

export async function getAuthProviders(): Promise<AuthProvidersResponse> {
  return request<AuthProvidersResponse>({ method: 'GET', url: '/auth/providers' });
}

export async function googleLogin(req: GoogleLoginRequest): Promise<LoginResponse> {
  return request<LoginResponse>({ method: 'POST', url: '/auth/google', data: req });
}

export async function signUp(req: SignUpRequest): Promise<LoginResponse> {
  return request<LoginResponse>({ method: 'POST', url: '/auth/signup', data: req });
}

export async function requestPasswordReset(email: string): Promise<MessageResponse> {
  return request<MessageResponse>({ method: 'POST', url: '/auth/forgot-password', data: { email } });
}

export async function resetPassword(req: ResetPasswordRequest): Promise<MessageResponse> {
  return request<MessageResponse>({ method: 'POST', url: '/auth/reset-password', data: req });
}

export async function getVehicles(): Promise<VehicleDto[]> {
  return request<VehicleDto[]>({ method: 'GET', url: '/vehicles' });
}

export async function createVehicle(req: VehicleInput): Promise<VehicleDto> {
  return request<VehicleDto>({ method: 'POST', url: '/vehicles', data: req });
}

export async function updateVehicle(id: string, req: VehicleInput): Promise<VehicleDto> {
  return request<VehicleDto>({ method: 'PUT', url: `/vehicles/${id}`, data: req });
}

export async function deleteVehicle(id: string): Promise<void> {
  await request<void>({ method: 'DELETE', url: `/vehicles/${id}` });
}

export async function getDrivers(): Promise<DriverDto[]> {
  return request<DriverDto[]>({ method: 'GET', url: '/vehicles/drivers' });
}

export async function getUsers(): Promise<UserDto[]> {
  return request<UserDto[]>({ method: 'GET', url: '/auth/users' });
}

export async function updateUserRole(id: string, req: UpdateUserRoleRequest): Promise<UserDto> {
  return request<UserDto>({ method: 'PUT', url: `/auth/users/${id}/role`, data: req });
}

export async function geocode(
  query: string,
): Promise<{ lat: number; lng: number; label?: string } | null> {
  const trimmed = query.trim();
  if (!trimmed) return null;
  try {
    const data = await request<GeocodeResultDto>({
      method: 'GET',
      url: '/geo/search',
      params: { q: trimmed },
    });
    return { lat: data.latitude, lng: data.longitude, label: data.displayName };
  } catch {
    return null;
  }
}

export async function getDrivingRoute(
  origin: { lat: number; lng: number },
  dest: { lat: number; lng: number },
  signal?: AbortSignal,
): Promise<[number, number][]> {
  const data = await request<RouteResultDto>({
    method: 'GET',
    url: '/geo/route',
    params: {
      originLat: origin.lat,
      originLng: origin.lng,
      destLat: dest.lat,
      destLng: dest.lng,
    },
    signal,
  });
  return (data.points ?? []).map((p) => [p.latitude, p.longitude]);
}
