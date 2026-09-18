// Mirrors Api/Contracts/DeliveryAgentRequests.cs and Application/Common/Models/SharedDtos.cs

export interface RegisterDeliveryAgentRequest {
  name: string;
  phone: string;
  password: string;
}

export interface UpdateAgentProfileRequest {
  name: string;
  phone: string;
}

export interface SetAgentAvailabilityRequest {
  isAvailable: boolean;
}

export interface DeliveryAgentDto {
  id: string;
  name: string;
  phone: string;
  isAvailable: boolean;
  createdAt: string;
}
