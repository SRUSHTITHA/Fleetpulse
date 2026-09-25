export const UserRole = {
  Admin: 'Admin',
  Dispatcher: 'Dispatcher',
  Driver: 'Driver',
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const VehicleType = {
  Van: 'Van',
  Truck: 'Truck',
  Motorcycle: 'Motorcycle',
  SmallTruck: 'SmallTruck',
  LargeContaineredTruck: 'LargeContaineredTruck',
} as const;
export type VehicleType = (typeof VehicleType)[keyof typeof VehicleType];

export const TripStatus = {
  Scheduled: 'Scheduled',
  InProgress: 'InProgress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
} as const;
export type TripStatus = (typeof TripStatus)[keyof typeof TripStatus];

export const ExceptionRuleType = {
  StopTooLong: 'StopTooLong',
  DeliveryTrendingLate: 'DeliveryTrendingLate',
  RouteDeviation: 'RouteDeviation',
  SpeedAnomaly: 'SpeedAnomaly',
} as const;
export type ExceptionRuleType = (typeof ExceptionRuleType)[keyof typeof ExceptionRuleType];

export const ExceptionSeverity = {
  Warning: 'Warning',
  Critical: 'Critical',
} as const;
export type ExceptionSeverity = (typeof ExceptionSeverity)[keyof typeof ExceptionSeverity];

export const ExceptionStatus = {
  Active: 'Active',
  Acknowledged: 'Acknowledged',
  Resolved: 'Resolved',
} as const;
export type ExceptionStatus = (typeof ExceptionStatus)[keyof typeof ExceptionStatus];

export const RULE_LABELS: Record<ExceptionRuleType, string> = {
  StopTooLong: 'Stop Too Long',
  DeliveryTrendingLate: 'Delivery Trending Late',
  RouteDeviation: 'Route Deviation',
  SpeedAnomaly: 'Speed Anomaly',
};

export const RULE_TYPES: ExceptionRuleType[] = [
  'StopTooLong',
  'DeliveryTrendingLate',
  'RouteDeviation',
  'SpeedAnomaly',
];

export const RULE_DESC: Record<ExceptionRuleType, string> = {
  StopTooLong: 'Vehicle stationary for too long',
  DeliveryTrendingLate: 'Running late vs. distance covered',
  RouteDeviation: 'Off the planned route',
  SpeedAnomaly: 'Exceeds max speed',
};

export const RULE_UNITS: Partial<Record<ExceptionRuleType, string>> = {
  StopTooLong: 'min',
  DeliveryTrendingLate: '%',
  RouteDeviation: 'm',
  SpeedAnomaly: 'km/h',
};

export interface ExceptionRuleDto {
  id: string;
  ruleType: ExceptionRuleType;
  thresholdMinutes?: number;
  thresholdPercent?: number;
  thresholdValue?: number;
  severity: ExceptionSeverity;
  isEnabled: boolean;
  createdBy: string;
  createdAt: string;
}

export interface CreateExceptionRuleRequest {
  ruleType: ExceptionRuleType;
  thresholdMinutes?: number;
  thresholdPercent?: number;
  thresholdValue?: number;
  severity: ExceptionSeverity;
  isEnabled: boolean;
}

export interface UpdateExceptionRuleRequest {
  thresholdMinutes?: number;
  thresholdPercent?: number;
  thresholdValue?: number;
  severity: ExceptionSeverity;
  isEnabled: boolean;
}

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  driverId?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SignUpRequest {
  email: string;
  password: string;
  fullName: string;
  licenseNumber?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface MessageResponse {
  message: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

export interface AuthProvidersResponse {
  googleClientId?: string;
}

export interface GeocodeResultDto {
  latitude: number;
  longitude: number;
  displayName?: string;
}

export interface RouteResultDto {
  points: Array<{ latitude: number; longitude: number }>;
}

export interface RoutePoint {
  lat: number;
  lng: number;
}

export interface UpdateUserRoleRequest {
  role: UserRole;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserDto;
}

export interface VehicleDto {
  id: string;
  name: string;
  licensePlate: string;
  type: VehicleType;
  isActive: boolean;
  latitude?: number;
  longitude?: number;
  assignedDriverId?: string;
  assignedDriverName?: string;
  lastLatitude?: number;
  lastLongitude?: number;
  lastSpeedKmh?: number;
  lastRecordedAt?: string;
}

export interface VehicleInput {
  name: string;
  licensePlate: string;
  type: VehicleType;
  isActive: boolean;
  driverId?: string | null;
  latitude?: number | null;
  longitude?: number | null;
}

export interface DriverDto {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  licenseNumber: string;
  isActive: boolean;
  vehicleId?: string;
  vehicleName?: string;
  licensePlate?: string;
}

export interface TripDto {
  id: string;
  driverId: string;
  vehicleId: string;
  status: TripStatus;
  origin: string;
  destination: string;
  originLat: number;
  originLng: number;
  destLat: number;
  destLng: number;
  estimatedDeparture: string;
  actualDeparture?: string;
  estimatedArrival: string;
  actualArrival?: string;
  createdAt: string;
  driverName?: string;
  vehicleName?: string;
  licensePlate?: string;
  lastLatitude?: number;
  lastLongitude?: number;
  lastSpeedKmh?: number;
  lastRecordedAt?: string;
}

export interface StartTripRequest {
  origin: string;
  destination: string;
  originLat: number;
  originLng: number;
  destLat: number;
  destLng: number;
  estimatedArrival: string;
}

export interface ReportLocationRequest {
  tripId: string;
  latitude: number;
  longitude: number;
  speedKmh?: number;
}

export type LocationReportStatus =
  | 'Accepted'
  | 'Throttled'
  | 'TripNotFound'
  | 'NotTripDriver'
  | 'TripNotInProgress';

export interface ReportLocationResponse {
  status: LocationReportStatus;
  message: string;
  location?: LocationDto;
}

export interface LocationDto {
  id: string;
  tripId: string;
  driverId: string;
  latitude: number;
  longitude: number;
  speedKmh?: number;
  recordedAt: string;
}

export interface LocationBroadcastDto {
  tripId: string;
  driverId: string;
  driverName: string;
  vehicleId: string;
  vehicleName: string;
  licensePlate: string;
  latitude: number;
  longitude: number;
  speedKmh?: number;
  recordedAt: string;
}

export interface MapMarker {
  key: string;
  tripId?: string;
  vehicleId: string;
  vehicleName: string;
  licensePlate: string;
  driverName: string;
  speedKmh?: number;
  latitude: number;
  longitude: number;
  recordedAt: string;
  source: 'trip' | 'vehicle';
}

export interface ExceptionAlertDto {
  id: string;
  tripId: string;
  driverId: string;
  driverName: string;
  vehicleName: string;
  licensePlate: string;
  origin: string;
  destination: string;
  ruleType: ExceptionRuleType;
  severity: ExceptionSeverity;
  status: ExceptionStatus;
  title: string;
  description: string;
  latitude: number;
  longitude: number;
  detectedAt: string;
}
