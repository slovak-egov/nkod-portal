import axios, { AxiosError, AxiosResponse } from 'axios';
import React, { useCallback, useContext, useEffect, useState } from 'react';
import { Dataset, useUserInfo } from './client';

const baseUrl = process.env.REACT_APP_CMS_API_URL ?? process.env.REACT_APP_API_URL + 'cms/';

console.log('baseUrl: ', baseUrl);

export type AutocompleteOption<T> = {
    value: T;
    label: string;
};

export type NewCmsUser = {
    firstName: string;
    lastName: string;
    userName: string;
    email: string | undefined;
    password: string;
    passwordConfirm: string;
};

export type CmsUserModel = {
    user: CmsUser;
    roles: CmsRole[];
    selectedRoles: string[] | null;
    password: string;
    passwordConfirm: string;
};

export type LoginPayload = {
    username: string;
    password: string;
};

export type AppRegistrationFormValues = {
    title: string;
    userId?: string;
    description: string;
    type: ApplicationType;
    theme: ApplicationTheme;
    url?: string | null;
    logo?: string;
    logoFiles: FileList | null;
    datasetURIsForm: { value: string }[];
    contactName: string;
    contactSurname: string;
    contactEmail: string;
};

export enum ApplicationType {
    MOBILE_APPLICATION = 'MA',
    WEB_APPLICATION = 'WA',
    WEB_PORTAL = 'WP',
    VISUALIZATION = 'V',
    ANALYSIS = 'A'
}

export enum ApplicationTheme {
    EDUCATION = 'ED',
    HEALTH = 'HE',
    ENVIRONMENT = 'EN',
    TRANSPORT = 'TR',
    CULTURE = 'CU',
    TOURISM = 'TU',
    ECONOMY = 'EC',
    SOCIAL = 'SO',
    PUBLIC_ADMINISTRATION = 'PA',
    OTHER = 'O'
}

export enum SuggestionType {
    SUGGESTION_FOR_PUBLISHED_DATASET = 'PN',
    SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET = 'DQ',
    SUGGESTION_FOR_QUALITY_OF_METADATA = 'MQ',
    SUGGESTION_OTHER = 'O'
}

export enum SuggestionStatusCode {
    CREATED = 'C',
    IN_PROGRESS = 'P',
    RESOLVED = 'R'
}

export interface Comment {
    id?: string;
    contentId: string;
    userId: string;
    author: string;
    email: string;
    body: string;
    created: string;
}

export interface CommentFormValues {
    body: string;
}

export interface SuggestionFormValues {
    userId?: string;
    userOrgUri?: string | null;
    orgToUri?: any;
    type: string;
    datasetUri: any;
    title: string;
    description: string;
    status: string;
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

export type OrganizationItem = {
    id: string;
    key: string;
    name: string;
    isPublic: boolean;
    datasetCount: number;
    themes?: {
        [key: string]: number | undefined;
    };
    nameAll: {
        sk: string;
    };
    website: any;
    email: any;
    phone: any;
    legalForm: any;
};

export type OrganizationList = {
    items: OrganizationItem[];
    facets: [];
    totalCount: number;
};

export type DatasetList = {
    items: Dataset[];
    facets: [];
    totalCount: number;
};

export interface EditSuggestionFormValues extends SuggestionFormValues {
    id: string;
    suggestionStatus: string;
}

export interface Audited {
    created: string;
    updated: string;
}

export interface Likeable {
    likeCount: number;
}

export interface Commentable {
    commentCount: number;
}

export interface Pageable<T> {
    items: T[];
    paginationMetadata: {
        totalItemCount: number;
        pageSize: number;
        currentPage: number;
    };
}

export interface Application extends Audited, Likeable {
    id: string;
    title: string;
    userId?: string;
    description: string;
    type: ApplicationType;
    theme: ApplicationTheme;
    url: string;
    logo: string;
    datasetURIs: string[];
    contactName: string;
    contactSurname: string;
    contactEmail: string;
}

export interface Suggestion extends SuggestionFormValues, Audited, Likeable, Commentable {
    id: string;
    suggestionStatus: string;
    createdDate: Date;
    createdBy?: string;
}

export interface CmsBaseModel {
    concurrencyStamp: string | undefined;
}

export interface CmsUser extends CmsBaseModel {
    id: string;
    userName: string | undefined;
    normalizedUserName: string | undefined;
    email: string | undefined;
    normalizedEmail: string | undefined;
    emailConfirmed: boolean;
    passwordHash: string | undefined;
    securityStamp: string | undefined;
    phoneNumber: string | undefined;
    phoneNumberConfirmed: boolean;
    twoFactorEnabled: boolean;
    lockoutEnd: Date | undefined;
    lockoutEnabled: boolean;
    accessFailedCount: number;
}

export interface CmsRole extends CmsBaseModel {
    id: string;
    name: string | undefined;
    normalizedName: string | undefined;
    concurrencyStamp: string | undefined;
}

export interface CmsUserContextType {
    cmsUser: CmsUser | null;
    setCmsUser: (user: CmsUser | null) => void;
}

export const CmsUserContext = React.createContext<CmsUserContextType | null>(null);
const createUser = (request: NewCmsUser): CmsUserModel => ({
    user: {
        id: '00000000-0000-0000-0000-000000000000',
        userName: request.userName,
        normalizedUserName: undefined,
        email: request.email,
        normalizedEmail: undefined,
        emailConfirmed: false,
        passwordHash: undefined,
        securityStamp: undefined,
        concurrencyStamp: undefined,
        phoneNumber: undefined,
        phoneNumberConfirmed: false,
        twoFactorEnabled: false,
        lockoutEnd: undefined,
        lockoutEnabled: false,
        accessFailedCount: 0
    },
    roles: [],
    selectedRoles: ['SysAdmin'],
    password: request.password,
    passwordConfirm: request.passwordConfirm
});

export function useCmsUserAdd(initialValue: NewCmsUser) {
    return useEntityAdd<CmsUserModel>('manager/user/save', createUser(initialValue));
}

export function useCmsUserLogin(initialValue: LoginPayload) {
    return useEntityAdd<LoginPayload>('user/login', initialValue);
}

export function useCmsUserLogout() {
    return useEntityAddWithoutInput('user/logout');
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
            return { success: true, data: response };
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

    const setEntityProperties = useCallback(
        (properties: Partial<T>) => {
            console.log('setEntityProperties');
            console.log('properties: ', properties);
            setEntity({ ...entity, ...properties });
        },
        [entity, setEntity]
    );

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
            return { success: true, data: response };
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

export async function sendPut<TInput>(url: string, input: TInput) {
    return await axios.put(baseUrl + url, input);
}

export async function sendPostWithoutInput(url: string) {
    return await axios.post(baseUrl + url);
}

export async function sendGet(url: string) {
    return await axios.get(baseUrl + url);
}

export async function getCmsUser(): Promise<CmsUser | null> {
    let response = await axios.get<CmsUser>(baseUrl + 'user/info');
    return response.status === 200 ? response.data : null;
}

export function useCmsUser() {
    return useContext(CmsUserContext)?.cmsUser ?? null;
}

export function useCmsPublisherLists({
    language,
    page = 1,
    pageSize = 10000,
    orderBy = 'name'
}: {
    language: string;
    page?: number;
    pageSize?: number;
    orderBy?: string;
}) {
    const [publishers, setPublishers] = useState<AutocompleteOption<any>[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        async function load() {
            setLoading(true);
            setError(null);
            setPublishers([]);
            try {
                const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                    language: language,
                    page: page,
                    pageSize: pageSize,
                    orderBy: orderBy
                });
                if (response.data.items.length > 0) {
                    setPublishers(
                        response.data.items
                            .map((item) => ({
                                value: item,
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

export function useCmsApplications() {
    const [items, setItems] = useState<Application[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async () => {
        setLoading(true);
        try {
            const response: AxiosResponse<Pageable<Application>> = await sendGet('cms/applications');
            setItems(response.data?.items?.sort(sortByUpdatedCreated) ?? []);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        refresh();
    }, [refresh]);

    return [items, loading, error, refresh] as const;
}

export function useCmsLike() {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [userInfo] = useUserInfo();

    const like = useCallback(
        async (url: string, id: string) => {
            setLoading(true);
            try {
                const response: AxiosResponse<Suggestion[]> = await sendPost(url, {
                    contentId: id,
                    userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b1aa87'
                });
                setLoading(false);
                return response.status === 200;
            } catch (err) {
                setLoading(false);
                if (err instanceof Error) {
                    setError(err);
                    console.error('Like error', err.message);
                }
                return false;
            }
        },
        [userInfo?.id]
    );

    return [loading, error, like] as const;
}

export function useCmsComments(contentId: string) {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [comments, setComments] = useState<Comment[]>([]);

    const load = useCallback(async (contentId: string) => {
        setLoading(true);
        try {
            const response: AxiosResponse<Comment[]> = await sendGet(`cms/comments?contentId=${contentId}`);
            if (response.status === 200) {
                setComments(response.data?.sort((a, b) => Number(a.created) - Number(b.created)));
            }
            setLoading(false);
            return;
        } catch (err) {
            setLoading(false);
            if (err instanceof Error) {
                setError(err);
                console.error('Load comments error', err.message);
            }
            return false;
        }
    }, []);

    useEffect(() => {
        load(contentId);
    }, [load, contentId]);

    return [comments, loading, error, load] as const;
}

export function useCmsSuggestions() {
    const [items, setItems] = useState<Suggestion[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const refresh = useCallback(async () => {
        setLoading(true);
        try {
            const response: AxiosResponse<Pageable<Suggestion>> = await sendGet('cms/suggestions');
            setItems(response.data?.items.sort(sortByUpdatedCreated) ?? []);
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
            setItems(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        refresh();
    }, [refresh]);

    return [items, loading, error, refresh] as const;
}

const sortByUpdatedCreated = (a: Audited, b: Audited) => {
    const getDate = (dateStr: string) => (dateStr ? new Date(dateStr).getTime() : 0);
    const aUpdated = getDate(a.updated);
    const bUpdated = getDate(b.updated);
    const aCreated = new Date(a.created).getTime();
    const bCreated = new Date(b.created).getTime();

    return bUpdated - aUpdated || aCreated - bCreated;
};

export const useCmsSearchPublisherLists = ({
    language,
    page = 1,
    pageSize = 50,
    orderBy = 'name',
    queryText
}: {
    language: 'sk' | 'en';
    page?: number;
    pageSize?: number;
    orderBy?: string;
    queryText: string;
}) => {
    const [publishers, setPublishers] = useState<AutocompleteOption<string>[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);

    const load = async (query: string) => {
        setLoading(true);
        setError(null);
        setPublishers([]);
        try {
            const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                language: language,
                page: page,
                pageSize: pageSize,
                orderBy: orderBy,
                queryText: queryText
            });
            if (response.data.items.length > 0) {
                setPublishers(
                    response.data.items
                        .map((item) => ({
                            value: item.id,
                            label: item.nameAll.sk
                        }))
                        .sort((a, b) => a.label.localeCompare(b.label))
                );
            } else {
                setPublishers([]);
            }
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load(queryText);
    }, [language, page, pageSize, orderBy, queryText]);

    return [publishers, loading, error] as const;
};

export const useSearchPublisher = ({ language, query }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [publishers, setPublishers] = useState<AutocompleteOption<string>[]>([]);

    const load = async (query: string, filters?: any, pageSize = 50) => {
        setLoading(true);
        setError(null);
        try {
            const response = await axios.post<OrganizationList>('https://wpnkod.informo.sk/publishers/search', {
                language: language,
                page: 1,
                filters,
                pageSize,
                orderBy: 'name',
                queryText: query
            });
            let data: AutocompleteOption<any>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item) => ({
                        value: item.key,
                        label: item.nameAll.sk
                    }))
                    .sort((a, b) => a.label.localeCompare(b.label));
            }
            setPublishers(data);
            return data as AutocompleteOption<string>[];
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load(query);
    }, [query]);

    return [publishers, loading, error, load] as const;
};

export const useSearchDataset = ({ language, query, filters }: { language: string; query: string; filters?: any; pageSize?: number }) => {
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<Error | null>(null);
    const [datasets, setDatasets] = useState<AutocompleteOption<string>[]>([]);

    const load = async (query: string, filters?: any, pageSize = 50) => {
        setLoading(true);
        setError(null);
        try {
            const response = await axios.post<DatasetList>('https://wpnkod.informo.sk/datasets/search', {
                language,
                page: 1,
                filters,
                pageSize,
                orderBy: 'name',
                queryText: query
            });
            let data: AutocompleteOption<string>[] = [];
            if (response.data.items.length > 0) {
                data = response.data.items
                    .map((item) => ({
                        value: item.key,
                        label: item.name ?? ''
                    }))
                    .sort((a, b) => a.label.localeCompare(b.label));
            }
            setDatasets(data);
            return data as AutocompleteOption<string>[];
        } catch (err) {
            if (err instanceof Error) {
                setError(err);
            }
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (filters) {
            load(query);
        }
    }, [query]);

    return [datasets, loading, error, load] as const;
};
