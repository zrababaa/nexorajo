import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type CompanyProfile = Schemas['CompanyProfileDto'];
export type CompanyDocument = Schemas['CompanyDocumentDto'];

export interface CompanyProfileFields {
  registrationId: string | null;
  companyName: string | null;
  address: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  description: string | null;
  logoPath: string | null;
}

@Injectable({ providedIn: 'root' })
export class CompanyProfileService {
  private readonly http = inject(HttpClient);

  get(): Promise<CompanyProfile> {
    return firstValueFrom(this.http.get<CompanyProfile>('/api/v1/company-profile'));
  }

  update(fields: CompanyProfileFields): Promise<CompanyProfile> {
    return firstValueFrom(this.http.put<CompanyProfile>('/api/v1/company-profile', fields));
  }

  activate(): Promise<CompanyProfile> {
    return firstValueFrom(this.http.post<CompanyProfile>('/api/v1/company-profile/activate', {}));
  }

  deactivate(): Promise<CompanyProfile> {
    return firstValueFrom(this.http.post<CompanyProfile>('/api/v1/company-profile/deactivate', {}));
  }

  uploadLogo(file: File): Promise<{ path?: string }> {
    const form = new FormData();
    form.set('file', file);
    return firstValueFrom(this.http.post<{ path?: string }>('/api/v1/company-profile/logo', form));
  }

  uploadDocument(file: File): Promise<{ path?: string }> {
    const form = new FormData();
    form.set('file', file);
    return firstValueFrom(this.http.post<{ path?: string }>('/api/v1/company-profile/documents/upload', form));
  }

  listDocuments(): Promise<CompanyDocument[]> {
    return firstValueFrom(this.http.get<CompanyDocument[]>('/api/v1/company-profile/documents'));
  }

  addDocument(fileName: string, filePath: string, fileSizeBytes: number): Promise<CompanyDocument> {
    return firstValueFrom(
      this.http.post<CompanyDocument>('/api/v1/company-profile/documents', { fileName, filePath, fileSizeBytes }),
    );
  }

  deleteDocument(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/company-profile/documents/${id}`));
  }
}
