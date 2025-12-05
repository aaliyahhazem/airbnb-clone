export interface User{
  id: string;
  email: string;
  userName: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  role: UserRole;
  avatar?: string;
}
export enum UserRole{
  GUEST = 'GUEST',
  HOST = 'HOST',
  ADMIN = 'ADMIN'
}

// Backend response wrapper
export interface BackendResponse<T> {
  result: T;
  errorMessage?: string;
  IsHaveErrorOrNo: boolean;
  TotalCount?: number;
}

// Login/Register response data
export interface LoginResponseData {
  user: User;
  token: string;
  isFirstLogin?: boolean;
}

// Full API response (wrapped in BackendResponse)
export interface AuthResponse extends BackendResponse<LoginResponseData> {}

export interface LoginCredentials{
  email:string;
  password:string;
}
export interface RegisterData{
  email: string;
  password: string;
  fullName: string;
  userName: string;
  firebaseUid: string; // will be "null" until firebase is implemented
}
export interface SocialLoginRequest {
  token: string;
}
