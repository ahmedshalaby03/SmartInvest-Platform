export interface AppUser {
  id: string;
  fullName: string;
  userName: string;
  email: string;
  phoneNumber: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateEmployee {
  fullName: string;
  userName: string;
  email: string;
  phoneNumber?: string | null;
  password: string;
  role: string;
}
