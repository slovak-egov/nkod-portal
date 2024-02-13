import axios, {AxiosError, AxiosResponse} from "axios";
import React, {useCallback, useContext, useState} from "react";

const baseUrl = process.env.REACT_APP_CMS_API_URL ?? process.env.REACT_APP_API_URL + 'cms/';

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

export function useEntityAdd<T>(url: string, initialValue: T) {
    const [entity, setEntity] = useState<T>(initialValue);
    const [genericError, setGenericError] = useState<Error | null>(null);
    const [saving, setSaving] = useState(false);

    const save = useCallback(async () => {
        setSaving(true);
        setGenericError(null);
        try {
            const response: AxiosResponse<any> = await sendPost(url, entity);
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