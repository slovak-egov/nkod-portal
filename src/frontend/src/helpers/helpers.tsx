import { AxiosResponse } from 'axios';
import React, { useCallback, useEffect, useState } from 'react';
import { FieldValues, Path, UseFormReturn } from 'react-hook-form';
import { sendGet } from '../cms';
import Loading from '../components/Loading';

interface IUseLoadData<TForm extends FieldValues> {
    form: UseFormReturn<TForm>;
    disabled: boolean;
    url: string;
    transform?: (data: any) => Promise<Partial<TForm>> | Partial<TForm>;
}

interface IQueryGuardProps<T> {
    loading: boolean;
    error: boolean;
    isNew?: boolean;
    data: T;
    ErrorElement?: React.ReactNode;
    children: ((data: Exclude<T, undefined>) => JSX.Element) | JSX.Element;
}

export const useLoadData = <TForm extends FieldValues, TData>(props: IUseLoadData<TForm>) => {
    const { transform, form, disabled, url } = props;
    const [data, setData] = useState<TData>();
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(false);

    const handleData = useCallback(async () => {
        if (!disabled) {
            setLoading(true);
            try {
                const response: AxiosResponse<TData> = await sendGet(url);
                if (response) {
                    const oneEntry = response.data;
                    if (response.data) {
                        setData(oneEntry);
                        let formData: Partial<TForm> = oneEntry as unknown as TForm;
                        if (transform) {
                            formData = await transform(oneEntry);
                        }

                        Object.keys(formData).forEach((key) => {
                            form.setValue(key as string as Path<TForm>, (formData as any)[key] ?? null);
                        });
                    }
                }
            } catch (err) {
                console.error('useLoadData', err);
                setError(true);
            } finally {
                setLoading(false);
            }
        }
    }, [form, disabled, url]);

    useEffect(() => {
        handleData();
    }, [handleData]);

    return { data, loading, error };
};

const isDefined = <T extends unknown>(data: T): data is Exclude<T, undefined> => {
    return data !== undefined;
};

export async function getBase64(file: File) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => {
            resolve(reader.result);
        };
        reader.onerror = reject;
    });
}

export function dataUrlToFileList(dataUrls: string[], fileNames: string[]): FileList {
    const fileArray: File[] = [];

    for (let index = 0; index < dataUrls.length; index++) {
        const dataUrl = dataUrls[index];
        const fileName = fileNames[index];
        const blobObject = dataUrlToBlob(dataUrl);
        if (blobObject) {
            const file = new File([blobObject], fileName);
            fileArray.push(file);
        }
    }

    return fileArray as unknown as FileList;
}

export function dataUrlToBlob(dataUrl: string): Blob | null {
    try {
        const byteString = atob(dataUrl.split(',')[1]);
        const mimeString = dataUrl.split(',')[0].split(':')[1].split(';')[0];
        const ab = new ArrayBuffer(byteString.length);
        const ia = new Uint8Array(ab);
        for (let i = 0; i < byteString.length; i++) {
            ia[i] = byteString.charCodeAt(i);
        }

        return new Blob([ia], { type: mimeString });
    } catch {
        return null;
    }
}

export const QueryGuard = <T extends unknown>(props: IQueryGuardProps<T>) => {
    const { loading, error, data, ErrorElement, children, isNew = false } = props;
    if (error && ErrorElement) {
        return <>ErrorElement</>;
    }
    if (isNew && typeof children !== 'function') {
        return children;
    }
    if (isDefined(data)) {
        return typeof children === 'function' ? children(data) : children;
    }
    if (loading) {
        return <Loading />;
    }
    return null;
};
