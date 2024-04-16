import axios, { AxiosError, AxiosResponse } from 'axios';
import React, { useCallback, useContext, useEffect, useState } from 'react';
import { CodelistValue } from './client';

const baseUrl = process.env.REACT_APP_CMS_API_URL ?? process.env.REACT_APP_API_URL + 'cms/';

console.log('baseUrl: ', baseUrl);

export type AutocompleteOption = {
    value: string,
    label: string
}

export type NewCmsUser = {
    firstName: string;
    lastName: string;
    userName: string;
    email: string | undefined;
    password: string;
    passwordConfirm: string;
}

export type CmsUserModel = {
    user: CmsUser,
    roles: CmsRole[],
    selectedRoles: string[] | null,
    password: string,
    passwordConfirm: string
}

export type LoginPayload = {
    username: string,
    password: string
}

export type AppRegistrationFormValues = {
    applicationName: string,
    applicationDescription: string,
    applicationType: string,
    applicationTheme: string,
    applicationUrl: string,
    applicationLogo: FileList | null,
    applicationDataset: {
        value: string
    }[],
    contactFirstName: string,
    contactLastName: string,
    contactEmail: string,
}

export enum ApplicationType {
    MOBILE_APPLICATION = 'MOBILE_APPLICATION',
    WEB_APPLICATION = 'WEB_APPLICATION',
    WEB_PORTAL = 'WEB_PORTAL',
    VISUALIZATION = 'VISUALIZATION',
    ANALYSIS = 'ANALYSIS',
}

export enum ApplicationTheme {
    EDUCATION = 'EDUCATION',
    HEALTH = 'HEALTH',
    ENVIRONMENT = 'ENVIRONMENT',
    TRANSPORT = 'TRANSPORT',
    CULTURE = 'CULTURE',
    TOURISM = 'TOURISM',
    ECONOMY = 'ECONOMY',
    SOCIAL = 'SOCIAL',
    PUBLIC_ADMINISTRATION = 'PUBLIC_ADMINISTRATION',
    OTHER = 'OTHER',
}

export enum SuggestionType {
    SUGGESTION_FOR_PUBLISHED_DATASET = 'SUGGESTION_FOR_PUBLISHED_DATASET',
    SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET = 'SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET',
    SUGGESTION_FOR_QUALITY_OF_METADATA = 'SUGGESTION_FOR_QUALITY_OF_METADATA',
    SUGGESTION_OTHER = 'SUGGESTION_OTHER',
}

export enum SuggestionStatusCode {
    PROPOSAL_FOR_CHANGE = 'PROPOSAL_FOR_CHANGE',
    PROPOSAL_FOR_CREATIOM = 'PROPOSAL_FOR_CREATIOM',
    PROPOSAL_REJECTED = 'PROPOSAL_REJECTED',
    PROPOSAL_APPROVED = 'PROPOSAL_APPROVED',
    PROPOSAL_IN_PROGRESS = 'PROPOSAL_IN_PROGRESS'
}

export interface SuggestionFormValues {
    organization: string,
    suggestionType: string,
    suggestionTitle: string,
    suggestionDescription: string,
}


export enum SuggestionOrganizationCode {
    MH = 'MH',
    MF = 'MF',
    MD = 'MD',
    MPRV = 'MPRV',
    MV = 'MV',
    MO = 'MO',
    MS = 'MS',
    MZVEZ = 'MZVEZ',
    MPSR = 'MPSR',
    MZP = 'MZP',
    MSVVS = 'MSVVS',
    MK = 'MK',
    MZ = 'MZ',
    MIRRI = 'MIRRI',
    MCRAS = 'MCRAS'
}

export type SuggestionOrganizationItem = {
    id: string
    key: string
    name: string
    isPublic: boolean
    datasetCount: number
    themes?: {
        [key: string]: number | undefined
    }
    nameAll: {
        sk: string
    }
    website: any
    email: any
    phone: any
    legalForm: any
}

export type SuggestionOrganizationList = {
    items: SuggestionOrganizationItem[]
}


export interface EditSuggestionFormValues extends SuggestionFormValues {
    id: string,
    suggestionStatus: string,
}

export interface Suggestion extends SuggestionFormValues {
    id: string,
    suggestionStatus: string,
    likeCount: number,
    commentCount: number,
    createdDate: Date,
    createdBy?: string,
}

export interface CmsBaseModel {
    concurrencyStamp: string | undefined,
}

export interface CmsUser extends CmsBaseModel {
    id: string,
    userName: string | undefined,
    normalizedUserName: string | undefined,
    email: string | undefined,
    normalizedEmail: string | undefined,
    emailConfirmed: boolean,
    passwordHash: string | undefined,
    securityStamp: string | undefined,
    phoneNumber: string | undefined,
    phoneNumberConfirmed: boolean,
    twoFactorEnabled: boolean,
    lockoutEnd: Date | undefined,
    lockoutEnabled: boolean,
    accessFailedCount: number
}

export interface CmsRole extends CmsBaseModel {
    id: string,
    name: string | undefined,
    normalizedName: string | undefined,
    concurrencyStamp: string | undefined
}

export interface CmsUserContextType {
    cmsUser: CmsUser | null;
    setCmsUser: (user: CmsUser|null) => void
}

export const CmsUserContext = React.createContext<CmsUserContextType | null>(null);
const createUser = (request: NewCmsUser): CmsUserModel => ({
    "user": {
        "id": "00000000-0000-0000-0000-000000000000",
        "userName": request.userName,
        "normalizedUserName": undefined,
        "email": request.email,
        "normalizedEmail": undefined,
        "emailConfirmed": false,
        "passwordHash": undefined,
        "securityStamp": undefined,
        "concurrencyStamp": undefined,
        "phoneNumber": undefined,
        "phoneNumberConfirmed": false,
        "twoFactorEnabled": false,
        "lockoutEnd": undefined,
        "lockoutEnabled": false,
        "accessFailedCount": 0
    },
    roles: [],
    "selectedRoles": ["SysAdmin"],
    "password": request.password,
    "passwordConfirm": request.passwordConfirm
})


export function useCmsUserAdd(initialValue: NewCmsUser) {
    return useEntityAdd<CmsUserModel>('manager/user/save', createUser(initialValue));
}

export function useCmsUserLogin(initialValue: LoginPayload) {
    return useEntityAdd<LoginPayload>('user/login', initialValue);
}

export function useCmsUserLogout() {
    return useEntityAddWithoutInput('user/logout');
}

// TODO: add api endpoint
export function useAppRegistration(initialValue: AppRegistrationFormValues) {
    console.log('useAppRegistration');
    return useEntityAdd<AppRegistrationFormValues>('manager/api/site/save', initialValue);
}

export function useEntityAdd<T>(url: string, initialValue: T) {
    const [entity, setEntity] = useState<T>(initialValue);
    const [genericError, setGenericError] = useState<Error | null>(null);
    const [saving, setSaving] = useState(false);

    console.log('url: ', url);
    console.log('initialValue: ', initialValue);

    const save = useCallback(async () => {
        setSaving(true);
        setGenericError(null);
        try {
            const response: AxiosResponse<any> = await sendPost(url, entity);
            console.log('response: ', response);
            return {success: true, data: response};
        } catch (err) {
            if (err instanceof AxiosError) {
                if (err.response?.data?.status?.type === 'danger') {
                    setGenericError({
                        name: err.response.data.status.body?.replace(/(<([^>]+)>)/gi, '') ?? 'Error',
                        message: ''
                    });
                }
            } else if (err instanceof Error) {
                setGenericError(err);
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [entity, url]);

    const setEntityProperties = useCallback((properties: Partial<T>) => {
        console.log('setEntityProperties');
        console.log('properties: ', properties);
        setEntity({...entity, ...properties});
    }, [entity, setEntity]);

    return [entity, setEntityProperties, genericError, saving, save] as const;
}

export function useEntityAddWithoutInput(url: string) {
    const [genericError, setGenericError] = useState<Error | null>(null);
    const [saving, setSaving] = useState(false);

    const save = useCallback(async () => {
        setSaving(true);
        setGenericError(null);
        try {
            const response: AxiosResponse<any> = await sendPostWithoutInput(url);
            return {success: true, data: response};
        } catch (err) {
            if (err instanceof AxiosError) {
                if (err.response?.data?.status?.type === 'danger') {
                    setGenericError({
                        name: err.response.data.status.body?.replace(/(<([^>]+)>)/gi, '') ?? 'Error',
                        message: ''
                    });
                }
            } else if (err instanceof Error) {
                setGenericError(err);
            }
        } finally {
            setSaving(false);
        }
        return null;
    }, [url]);

    return [genericError, saving, save] as const;
}

export async function sendPost<TInput>(url: string, input: TInput) {
    return await axios.post(baseUrl + url, input);
}

export async function sendPostWithoutInput(url: string) {
    return await axios.post(baseUrl + url);
}

export async function getCmsUser(): Promise<CmsUser | null> {
    let response = await axios.get<CmsUser>(baseUrl + 'user/info');
    return response.status === 200 ? response.data : null;
}

export function useCmsUser() {
    return useContext(CmsUserContext)?.cmsUser ?? null;
}

export function useCmsPublisherLists({ language, page = 1, pageSize = 10000, orderBy = 'name' }: {
    language: string
    page?: number
    pageSize?: number
    orderBy?: string
}) {
    const [publishers, setPublishers] = useState<AutocompleteOption[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setPublishers([]);
            try {
                const response = await axios.post<SuggestionOrganizationList>('https://wpnkod.informo.sk/publishers/search',
                    {
                        language: language,
                        page: page,
                        pageSize: pageSize,
                        orderBy: orderBy
                    }
                );
                if (response.data.items.length > 0) {
                    setPublishers(
                        response.data.items
                            .map(item => ({
                                value: item.id,
                                label: item.nameAll.sk
                            }))
                            .sort((a, b) => a.label.localeCompare(b.label))
                    );
                }
            } catch (err) {
                if (err instanceof Error) {
                    setError(err);
                }
            } finally {
                setLoading(false);
            }
        }

        load();
    }, [language, page, pageSize, orderBy]);

    return [publishers, loading, error] as const;
}
