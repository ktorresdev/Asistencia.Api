export interface LoginRequest {
  username: string;
  password: string;
}

// Matches AuthResponse record from the backend (camelCase JSON)
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  role: string;
  user?: {
    id: number;
    username: string;
    email?: string;
    role: string;
  };
  trabajador?: {
    id: number;
    sucursalId?: number;
    cargo?: string;
    areaDepartamento?: string;
  };
  persona?: {
    id: number;
    apellidosNombres: string;
    dni: string;
  };
}

export interface CurrentUser {
  username: string;
  role: string;
  sucursalId?: number;
  trabajadorId?: number;
  personaNombre?: string;
}

// Backend RefreshRequest record only needs { refreshToken }
export interface RefreshTokenRequest {
  refreshToken: string;
}
