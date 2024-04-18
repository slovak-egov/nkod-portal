import React, { useContext, useState } from 'react';
import { Controller, SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import PageHeader from '../components/PageHeader';
import Button from '../components/Button';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import { CodelistValue, TokenContext, useDocumentTitle, useUserInfo } from '../client';
import FormElementGroup from '../components/FormElementGroup';
import BaseInput from '../components/BaseInput';
import TextArea from '../components/TextArea';
import FileUpload from '../components/FileUpload';
import { ApplicationTheme, ApplicationType, AppRegistrationFormValues, sendPost } from '../cms';
import SelectElementItems from '../components/SelectElementItems';
import GridRow from '../components/GridRow';
import GridColumn from '../components/GridColumn';

type ProfileOptions = {
    website: string;
    email: string;
    phone: string;
}

const applicationTypeCodeList: CodelistValue[] = [
    {
        id: ApplicationType.MOBILE_APPLICATION,
        label: 'mobilná aplikácia'
    },
    {
        id: ApplicationType.WEB_APPLICATION,
        label: 'webová aplikácia'
    },
    {
        id: ApplicationType.WEB_PORTAL,
        label: 'webový portál'
    },
    {
        id: ApplicationType.VISUALIZATION,
        label: 'vizualizácia'
    },
    {
        id: ApplicationType.ANALYSIS,
        label: 'analýza'
    }
];

const applicationThemeCodeList: CodelistValue[] = [
    {
        id: ApplicationTheme.EDUCATION,
        label: 'školstvo'
    },
    {
        id: ApplicationTheme.HEALTH,
        label: 'zdravotníctvo'
    },
    {
        id: ApplicationTheme.ENVIRONMENT,
        label: 'životné prostredie'
    },
    {
        id: ApplicationTheme.TRANSPORT,
        label: 'doprava'
    },
    {
        id: ApplicationTheme.CULTURE,
        label: 'kultúra'
    },
    {
        id: ApplicationTheme.TOURISM,
        label: 'cestovný ruch'
    },
    {
        id: ApplicationTheme.ECONOMY,
        label: 'ekonomika'
    },
    {
        id: ApplicationTheme.SOCIAL,
        label: 'sociálne veci'
    },
    {
        id: ApplicationTheme.PUBLIC_ADMINISTRATION,
        label: 'verejná správa'
    },
    {
        id: ApplicationTheme.OTHER,
        label: 'ostatné'
    }
];

export default function AddApplication() {
    // const [profile, setProfile] = useState<ProfileOptions | null>(null);
    const [saving, setSaving] = useState<boolean>();
    // const [saveResult, setSaveResult] = useState<SaveResult | null>(null);
    const [userInfo] = useUserInfo();
    // const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('addApplicationPage.headerTitle'));
    const tokenContext = useContext(TokenContext);
    // const [uploading, upload] = useAppRegistrationFileUpload();

    const { control, register, handleSubmit, watch, formState: { errors } } = useForm<AppRegistrationFormValues>({
        defaultValues: {
            applicationDataset: [
                {
                    value: ''
                }
            ]
        }
    });

    // const [application, setApplication, genericError, saving, save] = useAppRegistration({
    //     applicationName: '',
    //     applicationDescription: '',
    //     applicationType: '',
    //     applicationUrl: '',
    //     applicationLogo: null,
    //     applicationDataset: [],
    //     contactFirstName: '',
    //     contactLastName: '',
    //     contactEmail: ''
    // });

    // const errors = saveResult?.errors ?? {};

    const onSubmit: SubmitHandler<AppRegistrationFormValues> = async (data) => {
        try {
            setSaving(true);
            const formData = new FormData();

            Object.entries(data).forEach(([key, value]) => {
                if (key === 'applicationLogo') {
                    if (value && Object.keys(value).length > 0){
                        const file = value as FileList;

                        formData.append(key, new Blob([file[0]], { type: file[0].type }));
                    }
                } else if (key === 'applicationDataset') {
                    (value as {value: string}[]).forEach((v, i) => {
                        formData.append(`${key}[${i}].value`, v.value);
                    });
                } else {
                    formData.append(key, value as string);
                }
            });

            console.log('onSubmit');
            console.log('data: ', data);
            const result = await sendPost<FormData>(`manager/api/site/save`, formData);

            console.log('result: ', result);

            // setApplication({
            //     applicationName: data.applicationName,
            // });
            // console.log('application: ', application);
            // save().then((result) => {
            //     console.log('result: ', result);
            // });
        } catch (error) {
            console.error('error: ', error);
        } finally {
            setSaving(false);
        }
    };

    const { fields, append, remove } = useFieldArray<AppRegistrationFormValues>({
        control,
        name: 'applicationDataset'
    });

    console.log('fields: ', fields);


    return <>
        <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('addApplicationPage.headerTitle') }]} />
        <MainContent>
            <form onSubmit={handleSubmit(onSubmit)}>
                <PageHeader>{t('addApplicationPage.title')}</PageHeader>
                {/*{profile ? <>*/}
                {/*    {Object.keys(errors).length > 0 ? <ValidationSummary elements={Object.entries(errors).map(k => ({*/}
                {/*        elementId: k[0],*/}
                {/*        message: k[1]*/}
                {/*    }))} /> : null}*/}

                <h2 className='govuk-heading-m '>
                    {t('addApplicationPage.applicationSubTitle')}
                </h2>

                <FormElementGroup
                    label={t('addApplicationPage.fields.applicationName')}
                    element={id =>
                        <BaseInput
                            id={id}
                            disabled={saving}
                            placeholder={t('addApplicationPage.fields.applicationName')}
                            {...register('applicationName')}
                        />
                    }
                />
                <FormElementGroup
                    label={t('addApplicationPage.fields.applicationDescription')}
                    element={id =>
                        <TextArea
                            id={id}
                            disabled={saving}
                            {...register('applicationDescription')}
                            placeholder={t('addApplicationPage.fields.applicationDescription')}
                        />
                    }
                />

                <Controller
                    render={({ field }) => (
                        <FormElementGroup
                            label={t('addApplicationPage.fields.applicationTheme')}
                            element={id => <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={applicationTypeCodeList}
                                selectedValue={field.value}
                                onChange={field.onChange}
                                renderOption={v => v.label}
                                getValue={v => v.id}
                            />}
                        />
                    )}
                    name='applicationType'
                    control={control}
                />

                <Controller
                    render={({ field }) => (
                        <FormElementGroup
                            label={t('addApplicationPage.fields.applicationType')}
                            element={id => <SelectElementItems<CodelistValue>
                                id={id}
                                disabled={saving}
                                options={applicationThemeCodeList}
                                selectedValue={field.value}
                                onChange={field.onChange}
                                renderOption={v => v.label}
                                getValue={v => v.id}
                            />}
                        />
                    )}
                    name='applicationTheme'
                    control={control}
                />


                <FormElementGroup
                    label={t('addApplicationPage.fields.applicationUrl')}
                    element={id =>
                        <BaseInput
                            id={id}
                            disabled={saving}
                            {...register('applicationUrl')}
                            placeholder='https://...'
                        />
                    }
                />

                <FormElementGroup
                    label={t('addApplicationPage.fields.applicationLogo')}
                    element={id =>
                        <FileUpload
                            id={id}
                            disabled={saving}
                            {...register('applicationLogo')}
                        />
                    }
                />

                {fields.map((field, index) => {
                    return (
                        <FormElementGroup
                            key={field.id}
                            label={t('addApplicationPage.fields.applicationDataset')}
                            element={id =>
                                <>
                                    <BaseInput
                                        id={id}
                                        disabled={saving}
                                        {...register(`applicationDataset.${index}.value`)}
                                        placeholder={t('addApplicationPage.fields.applicationDataset')}
                                    />
                                    {fields.length > 1 && (
                                        <Button onClick={() => remove(index)}>
                                            <svg width='20' height='5' viewBox='0 0 20 5' fill='none'
                                                 xmlns='http://www.w3.org/2000/svg'>
                                                <path d='M10 0.5V4M20 2.3382L0 2.3382' stroke='#0B0C0C'
                                                      stroke-width='4' />
                                            </svg>
                                        </Button>
                                    )}
                                </>
                            }
                        />
                    );
                })}
                <Button onClick={() => append({ value: '' })}>
                    <svg width='20' height='20' viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'>
                        <path d='M8 10H11.5M9.8382 0L9.8382 20' stroke='#0B0C0C' stroke-width='4' />
                        <path d='M10 8V11.5M20 9.8382L0 9.8382' stroke='#0B0C0C' stroke-width='4' />
                    </svg>
                </Button>


                <h2 className='govuk-heading-m '>
                    {t('addApplicationPage.contactSubTitle')}
                </h2>


                <FormElementGroup
                    label={t('addApplicationPage.fields.contactFirstName')}
                    element={id =>
                        <BaseInput
                            id={id}
                            disabled={saving}
                            {...register('contactFirstName')}
                            placeholder={t('addApplicationPage.fields.contactFirstName')}
                        />
                    }
                />
                <FormElementGroup
                    label={t('addApplicationPage.fields.contactLastName')}
                    element={id =>
                        <BaseInput
                            id={id}
                            disabled={saving}
                            {...register('contactLastName')}
                            placeholder={t('addApplicationPage.fields.contactLastName')}
                        />
                    }
                />
                <FormElementGroup
                    label={t('addApplicationPage.fields.contactEmail')}
                    element={id =>
                        <BaseInput
                            id={id}
                            type='email'
                            disabled={saving}
                            {...register('contactEmail')}
                            placeholder={t('addApplicationPage.fields.contactEmail')}
                        />
                    }
                />


                <GridRow>
                    <GridColumn widthUnits={1} totalUnits={2}>
                        <Button disabled={saving} type={'submit'}>
                            {t('addApplicationPage.draftButton')}
                        </Button>
                    </GridColumn>
                    {/*<GridColumn widthUnits={1} totalUnits={2} style={{ display: 'flex', justifyContent: 'flex-end' }}>*/}
                    {/*    <Button onClick={save} disabled={saving}*/}
                    {/*            buttonType={'secondary'}>*/}
                    {/*        {t('addApplicationPage.addButton')}*/}
                    {/*    </Button>*/}
                    {/*</GridColumn>*/}
                </GridRow>
                {/*</> : null}*/}
            </form>
        </MainContent>
    </>
        ;
}
