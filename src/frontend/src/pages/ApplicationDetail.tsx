import { useContext, useRef, useState } from 'react';
import { Controller, SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import { yupResolver } from '@hookform/resolvers/yup';
import classNames from 'classnames';
import { useNavigate, useParams } from 'react-router';
import { buildYup } from 'schema-to-yup';
import { CodelistValue, TokenContext, useDocumentTitle, useUserInfo } from '../client';
import { AppRegistrationFormValues, Application, ApplicationTheme, ApplicationType, sendPost, sendPut } from '../cms';
import BaseInput from '../components/BaseInput';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FileUpload from '../components/FileUpload';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import SelectElementItems from '../components/SelectElementItems';
import TextArea from '../components/TextArea';
import { QueryGuard, dataUrlToFileList, getBase64, useLoadData } from '../helpers/helpers';
import { schema, schemaConfig } from './schemas/ApplicationSchema';
import CommentSection from './CommentSection';

// TODO:
// dokončiť preklady
// ? ukladanie filename alebo pracné mapovanie na mimetype?
// ? zobrazenie valiadčných chýb nejako inak ako v konzole
// ? Vymyslieť spôsob na parsovanie JSON schema (možno lib)

export const applicationTypeCodeList: CodelistValue[] = [
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

type Props = {
    readonly?: boolean;
};

export default function ApplicationDetail(props: Props) {
    // const [profile, setProfile] = useState<ProfileOptions | null>(null);
    // const [saveResult, setSaveResult] = useState<SaveResult | null>(null);
    // const headers = useDefaultHeaders();
    const [userInfo] = useUserInfo();
    const { readonly } = props;
    const commentsRef = useRef(null);
    const tokenContext = useContext(TokenContext);
    const [saving, setSaving] = useState<boolean>();
    const { t } = useTranslation();
    const navigate = useNavigate();
    const { id } = useParams();
    useDocumentTitle(t('addApplicationPage.headerTitle'));
    const yupSchema = buildYup(schema, schemaConfig);

    const form = useForm<AppRegistrationFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b2ae87',
            datasetURIsForm: [{ value: '' }],
            type: ApplicationType.MOBILE_APPLICATION,
            theme: ApplicationTheme.EDUCATION,
            url: null
        }
    });

    const {
        control,
        register,
        handleSubmit,
        setValue,
        getValues,
        watch,
        formState: { errors }
    } = form;

    const loadFormData = useLoadData<any, Application>({
        disabled: !id,
        form,
        url: `cms/applications/${id}`,
        transform: (data: Application) => {
            const formData = {
                ...data,
                logoFiles: data.logo ? dataUrlToFileList([data.logo], ['logo.png']) : null, //TODO: GENEROVAT SPRAVNE FILENAME A KONCOVKY
                datasetURIsForm: data.datasetURIs?.length > 0 ? data.datasetURIs?.map((dataset) => ({ value: dataset })) : [{ value: '' }]
            };
            return formData;
        }
    });

    const onSubmit: SubmitHandler<AppRegistrationFormValues> = async (data) => {
        try {
            const save = async (logo: string | null = null) => {
                setSaving(true);
                const { datasetURIsForm, logoFiles, ...rest } = data;

                const request = {
                    ...rest,
                    id,
                    logo,
                    url: data.url ? data.url : null,
                    datasetURIs: data.datasetURIsForm?.map((dataset) => dataset.value) ?? []
                };

                let result = null;
                if (id) {
                    result = await sendPut<any>(`cms/applications/${id}`, request);
                } else {
                    result = await sendPost<any>(`cms/applications`, request);
                }
                if (result?.status === 200) {
                    navigate('/odkomunita', { state: { info: id ? t('applicationEditSuccessful') : t('applicationAddSuccessful') } });
                }
            };

            if (data.logoFiles?.length) {
                await getBase64((data.logoFiles as FileList)?.[0])
                    .then((base64) => save(base64 as string))
                    .catch((err) => console.error('logo error: ', err));
            } else {
                save();
            }
        } catch (error) {
            console.error('error: ', error);
        } finally {
            setSaving(false);
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    const { fields, append, remove } = useFieldArray<AppRegistrationFormValues>({
        control,
        name: 'datasetURIsForm'
    });

    return (
        <>
            <Breadcrumbs
                items={[
                    { title: t('nkod'), link: '/' },
                    { title: t('applicationList.headerTitle'), link: '/aplikacia' },
                    { title: t('addApplicationPage.headerTitle') }
                ]}
            />
            <QueryGuard {...loadFormData} isNew={!id}>
                <MainContent>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={1}>
                            <PageHeader size="l">{t('addApplicationPage.title')}</PageHeader>
                        </GridColumn>
                        <GridColumn widthUnits={2} totalUnits={3}>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <h2 className="govuk-heading-m ">{t('addApplicationPage.applicationSubTitle')}</h2>

                                <FormElementGroup
                                    label={t('addApplicationPage.fields.applicationName')}
                                    element={(id) => <BaseInput id={id} disabled={saving} readOnly={readonly} {...register('title')} />}
                                />
                                <FormElementGroup
                                    label={t('addApplicationPage.fields.applicationDescription')}
                                    element={(id) => <TextArea id={id} disabled={saving} readOnly={readonly} {...register('description')} />}
                                />

                                <Controller
                                    render={({ field }) => (
                                        <FormElementGroup
                                            label={t('addApplicationPage.fields.applicationTheme')}
                                            element={(id) => (
                                                <SelectElementItems<CodelistValue>
                                                    id={id}
                                                    disabled={saving || readonly}
                                                    options={applicationThemeCodeList}
                                                    selectedValue={field.value}
                                                    onChange={field.onChange}
                                                    renderOption={(v) => v.label}
                                                    getValue={(v) => v.id}
                                                />
                                            )}
                                        />
                                    )}
                                    name="theme"
                                    control={control}
                                />

                                <Controller
                                    render={({ field }) => (
                                        <FormElementGroup
                                            label={t('addApplicationPage.fields.applicationType')}
                                            element={(id) => (
                                                <SelectElementItems<CodelistValue>
                                                    id={id}
                                                    disabled={saving || readonly}
                                                    options={applicationTypeCodeList}
                                                    selectedValue={field.value}
                                                    onChange={field.onChange}
                                                    renderOption={(v) => v.label}
                                                    getValue={(v) => v.id}
                                                />
                                            )}
                                        />
                                    )}
                                    name="type"
                                    control={control}
                                />

                                <FormElementGroup
                                    label={t('addApplicationPage.fields.applicationUrl')}
                                    element={(id) => <BaseInput id={id} disabled={saving} readOnly={readonly} {...register('url')} placeholder="https://..." />}
                                />

                                {watch('logo') && (
                                    <>
                                        <label className="govuk-label">{t('addApplicationPage.fields.applicationLogo')}</label>
                                        <img src={getValues('logo')} width="200px" alt={t('addApplicationPage.fields.applicationLogo')} />
                                        {!readonly && (
                                            <Button
                                                buttonType="secondary"
                                                title={t('addApplicationPage.fields.applicationLogoRemove')}
                                                onClick={() => {
                                                    setValue('logo', undefined);
                                                    setValue('logoFiles', null);
                                                }}
                                            >
                                                <svg width="20" height="20" viewBox="0 0 20 5" fill="none" xmlns="http://www.w3.org/2000/svg">
                                                    <path d="M10 0.5V4M20 2.3382L0 2.3382" stroke="#0B0C0C" strokeWidth="4" />
                                                </svg>
                                            </Button>
                                        )}
                                    </>
                                )}

                                {!readonly && (
                                    <FormElementGroup
                                        label={t(`addApplicationPage.fields.applicationLogo${getValues('logoFiles') ? 'Change' : ''}`)}
                                        element={(id) => <FileUpload id={id} disabled={saving} {...register('logoFiles')} />}
                                    />
                                )}

                                {fields.map((field, index) => {
                                    return (
                                        <FormElementGroup
                                            key={field.id}
                                            label={t('addApplicationPage.fields.applicationDataset')}
                                            element={(id) => (
                                                <>
                                                    <GridRow>
                                                        <GridColumn widthUnits={3} totalUnits={4}>
                                                            <BaseInput
                                                                id={id}
                                                                disabled={saving}
                                                                readOnly={readonly}
                                                                {...register(`datasetURIsForm.${index}.value`)}
                                                            />
                                                        </GridColumn>
                                                        <GridColumn widthUnits={1} totalUnits={4}>
                                                            {fields.length > 1 && !readonly && (
                                                                <Button buttonType="secondary" onClick={() => remove(index)}>
                                                                    <svg
                                                                        width="20"
                                                                        height="20"
                                                                        viewBox="0 0 20 5"
                                                                        fill="none"
                                                                        xmlns="http://www.w3.org/2000/svg"
                                                                    >
                                                                        <path d="M10 0.5V4M20 2.3382L0 2.3382" stroke="#0B0C0C" strokeWidth="4" />
                                                                    </svg>
                                                                </Button>
                                                            )}
                                                            {fields.length - 1 === index && !readonly && (
                                                                <Button
                                                                    buttonType="secondary"
                                                                    onClick={() => append({ value: '' })}
                                                                    className={classNames({ 'govuk-!-margin-left-5': index !== 0 })}
                                                                >
                                                                    <svg
                                                                        width="20"
                                                                        height="20"
                                                                        viewBox="0 0 20 20"
                                                                        fill="none"
                                                                        xmlns="http://www.w3.org/2000/svg"
                                                                    >
                                                                        <path d="M8 10H11.5M9.8382 0L9.8382 20" stroke="#0B0C0C" strokeWidth="4" />
                                                                        <path d="M10 8V11.5M20 9.8382L0 9.8382" stroke="#0B0C0C" strokeWidth="4" />
                                                                    </svg>
                                                                </Button>
                                                            )}
                                                        </GridColumn>
                                                    </GridRow>
                                                </>
                                            )}
                                        />
                                    );
                                })}

                                <h2 className="govuk-heading-m ">{t('addApplicationPage.contactSubTitle')}</h2>

                                <FormElementGroup
                                    label={t('addApplicationPage.fields.contactFirstName')}
                                    element={(id) => <BaseInput id={id} disabled={saving} readOnly={readonly} {...register('contactName')} />}
                                />
                                <FormElementGroup
                                    label={t('addApplicationPage.fields.contactLastName')}
                                    element={(id) => <BaseInput id={id} disabled={saving} readOnly={readonly} {...register('contactSurname')} />}
                                />
                                <FormElementGroup
                                    label={t('addApplicationPage.fields.contactEmail')}
                                    element={(id) => <BaseInput id={id} type="email" disabled={saving} readOnly={readonly} {...register('contactEmail')} />}
                                />

                                {!readonly && (
                                    <GridRow>
                                        <GridColumn widthUnits={1} totalUnits={2}>
                                            <Button disabled={saving} type={'submit'}>
                                                {t('addApplicationPage.saveButton')}
                                            </Button>
                                        </GridColumn>
                                    </GridRow>
                                )}
                            </form>
                        </GridColumn>
                    </GridRow>
                </MainContent>
            </QueryGuard>
            <div ref={commentsRef}>{id && <CommentSection contentId={id} />}</div>
        </>
    );
}
ApplicationDetail.defaultProps = {
    readonly: false
};
