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

  private base(accountId: number): string {
    return `/api/v1/accounts/${accountId}/company-profile`;
  }

  get(accountId: number): Promise<CompanyProfile> {
    return firstValueFrom(this.http.get<CompanyProfile>(this.base(accountId)));
  }

  update(accountId: number, fields: CompanyProfileFields): Promise<CompanyProfile> {
    return firstValueFrom(this.http.put<CompanyProfile>(this.base(accountId), fields));
  }

  activate(accountId: number): Promise<CompanyProfile> {
    return firstValueFrom(this.http.post<CompanyProfile>(`${this.base(accountId)}/activate`, {}));
  }

  deactivate(accountId: number): Promise<CompanyProfile> {
    return firstValueFrom(this.http.post<CompanyProfile>(`${this.base(accountId)}/deactivate`, {}));
  }

  uploadLogo(accountId: number, file: File): Promise<{ path?: string }> {
    const form = new FormData();
    form.set('file', file);
    return firstValueFrom(this.http.post<{ path?: string }>(`${this.base(accountId)}/logo`, form));
  }

  uploadDocument(accountId: number, file: File): Promise<{ path?: string }> {
    const form = new FormData();
    form.set('file', file);
    return firstValueFrom(this.http.post<{ path?: string }>(`${this.base(accountId)}/documents/upload`, form));
  }

  listDocuments(accountId: number): Promise<CompanyDocument[]> {
    return firstValueFrom(this.http.get<CompanyDocument[]>(`${this.base(accountId)}/documents`));
  }

  addDocument(accountId: number, fileName: string, filePath: string, fileSizeBytes: number): Promise<CompanyDocument> {
    return firstValueFrom(
      this.http.post<CompanyDocument>(`${this.base(accountId)}/documents`, { fileName, filePath, fileSizeBytes }),
    );
  }

  deleteDocument(accountId: number, id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base(accountId)}/documents/${id}`));
  }
}
