import { useCallback, useRef, useState } from 'react';
import { Controller, SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import { yupResolver } from '@hookform/resolvers/yup';
import classNames from 'classnames';
import { useNavigate, useParams } from 'react-router';
import { buildYup } from 'schema-to-yup';
import { CodelistValue, useDefaultHeaders, useDocumentTitle, useUserInfo, useUserPermissions } from '../client';
import { sendCmsDelete, sendCmsPost, sendCmsPut } from '../cms';

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
import { QueryGuard, dataUrlToFileList, getBase64, getCodeListValues, useLoadData, useSchemaConfig } from '../helpers/helpers';
import { AppRegistrationFormValues, Application, ApplicationTheme, ApplicationType } from '../interface/cms.interface';
import CommentSection from './CommentSection';
import SuccessErrorPage from './SuccessErrorPage';
import { schema } from './schemas/ApplicationSchema';

export default function ApplicationForm() {
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const commentsRef = useRef(null);
    const { id } = useParams();
    const [saving, setSaving] = useState<boolean>();
    const [editable, setEditable] = useState<boolean>(!Boolean(id));
    const [saveSuccess, setSaveSuccess] = useState<boolean>(false);
    const { isSuperAdmin, isMine } = useUserPermissions();
    const { t } = useTranslation();
    const navigate = useNavigate();
    useDocumentTitle(t('addApplicationPage.headerTitle'));
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));

    const form = useForm<AppRegistrationFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            userId: userInfo?.id,
            datasetURIsForm: [{ value: '' }],
            type: ApplicationType.MOBILE_APPLICATION,
            theme: ApplicationTheme.EDUCATION,
            url: null,
            contactName: userInfo?.firstName,
            contactSurname: userInfo?.lastName,
            contactEmail: userInfo?.email
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
        url: `applications/${id}`,
        transform: (data: Application) => {
            const formData = {
                ...data,
                logoFiles: data.logo ? dataUrlToFileList([data.logo], [data.logoFileName]) : null,
                datasetURIsForm: data.datasetURIs?.length > 0 ? data.datasetURIs?.map((dataset) => ({ value: dataset })) : [{ value: '' }]
            };

            //All users can edit their own suggestions while in created state
            setEditable(isSuperAdmin || isMine(data.userId));
            return formData;
        }
    });

    const deleteApplication = useCallback(async () => {
        const result = await sendCmsDelete(`applications/${id}`, headers);
        if (result?.status === 200) {
            navigate('/aplikacia');
        }
    }, [id, navigate, headers]);

    const onSubmit: SubmitHandler<AppRegistrationFormValues> = async (data) => {
        try {
            const save = async (logo: string | null = null) => {
                setSaving(true);
                const { datasetURIsForm, logoFiles, ...rest } = data;

                const request = {
                    ...rest,
                    id,
                    logo,
                    logoFileName: data.logoFiles?.length ? data.logoFiles[0]?.name : null,
                    url: data.url ? data.url : null,
                    datasetURIs: data.datasetURIsForm?.map((dataset) => dataset.value) ?? []
                };

                let result = null;
                if (id) {
                    result = await sendCmsPut<any>(`applications/${id}`, request, headers);
                } else {
                    result = await sendCmsPost<any>(`applications`, request, headers);
                }
                if (result?.status === 200) {
                    setSaveSuccess(true);
                }
            };

            if (data.logoFiles?.length) {
                await getBase64((data.logoFiles as FileList)?.[0])
                    .then((base64) => save(base64 as string))
                    .catch((err) => console.error('Logo error: ', err));
            } else {
                save();
            }
        } catch (error) {
            console.error('Saving error: ', error);
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
            {saveSuccess ? (
                <SuccessErrorPage
                    msg={id ? t('applicationEditSuccessful') : t('applicationAddSuccessful')}
                    backButtonLabel={t('common.backToList')}
                    backButtonClick={() => navigate('/aplikacia')}
                />
            ) : (
                <>
                    <QueryGuard {...loadFormData} isNew={!id}>
                        <>
                            <Breadcrumbs
                                items={[
                                    { title: t('nkod'), link: '/' },
                                    { title: t('applicationList.headerTitle'), link: '/aplikacia' },
                                    { title: t('addApplicationPage.headerTitle') }
                                ]}
                            />

                            <MainContent>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <PageHeader size="l">{t('addApplicationPage.title')}</PageHeader>
                                    </GridColumn>
                                    <GridColumn widthUnits={2} totalUnits={3}>
                                        <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                            <h2 className="govuk-heading-m">{t('addApplicationPage.applicationSubTitle')}</h2>

                                            <FormElementGroup
                                                label={t('addApplicationPage.fields.applicationName')}
                                                errorMessage={errors.title?.message}
                                                element={(id) => <BaseInput id={id} disabled={saving || !editable} {...register('title')} />}
                                            />
                                            <FormElementGroup
                                                label={t('addApplicationPage.fields.applicationDescription')}
                                                errorMessage={errors.description?.message}
                                                element={(id) => <TextArea id={id} disabled={saving || !editable} {...register('description')} />}
                                            />

                                            <Controller
                                                render={({ field }) => (
                                                    <FormElementGroup
                                                        label={t('addApplicationPage.fields.applicationType')}
                                                        errorMessage={errors.type?.message}
                                                        element={(id) => (
                                                            <SelectElementItems<CodelistValue>
                                                                id={id}
                                                                disabled={saving || !editable}
                                                                options={getCodeListValues(t, ApplicationType, 'codelists.applicationType.')}
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

                                            <Controller
                                                render={({ field }) => (
                                                    <FormElementGroup
                                                        label={t('addApplicationPage.fields.applicationTheme')}
                                                        errorMessage={errors.theme?.message}
                                                        element={(id) => (
                                                            <SelectElementItems<CodelistValue>
                                                                id={id}
                                                                disabled={saving || !editable}
                                                                options={getCodeListValues(t, ApplicationTheme, 'codelists.applicationTheme.')}
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

                                            <FormElementGroup
                                                label={t('addApplicationPage.fields.applicationUrl')}
                                                element={(id) => (
                                                    <BaseInput id={id} disabled={saving || !editable} {...register('url')} placeholder="https://..." />
                                                )}
                                            />

                                            {watch('logo') && (
                                                <>
                                                    <label className="govuk-label">{t('addApplicationPage.fields.applicationLogo')}</label>
                                                    <img src={getValues('logo')} width="200px" alt={t('addApplicationPage.fields.applicationLogo')} />
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
                                                </>
                                            )}

                                            <FormElementGroup
                                                label={t(`addApplicationPage.fields.applicationLogo${getValues('logoFiles') ? 'Change' : ''}`)}
                                                element={(id) => <FileUpload id={id} disabled={saving || !editable} {...register('logoFiles')} />}
                                            />

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
                                                                            disabled={saving || !editable}
                                                                            {...register(`datasetURIsForm.${index}.value`)}
                                                                        />
                                                                    </GridColumn>
                                                                    <GridColumn widthUnits={1} totalUnits={4}>
                                                                        {fields.length > 1 && (
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
                                                                        {fields.length - 1 === index && (
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
                                                errorMessage={errors.contactName?.message}
                                                element={(id) => <BaseInput id={id} disabled={saving || !editable} {...register('contactName')} />}
                                            />
                                            <FormElementGroup
                                                label={t('addApplicationPage.fields.contactLastName')}
                                                errorMessage={errors.contactSurname?.message}
                                                element={(id) => <BaseInput id={id} disabled={saving || !editable} {...register('contactSurname')} />}
                                            />
                                            <FormElementGroup
                                                label={t('addApplicationPage.fields.contactEmail')}
                                                errorMessage={errors.contactEmail?.message}
                                                element={(id) => (
                                                    <BaseInput id={id} type="email" disabled={saving || !editable} {...register('contactEmail')} />
                                                )}
                                            />

                                            <GridRow>
                                                {editable && (
                                                    <GridColumn widthUnits={1} totalUnits={2}>
                                                        <Button disabled={saving} type={'submit'}>
                                                            {t('addApplicationPage.saveButton')}
                                                        </Button>
                                                    </GridColumn>
                                                )}
                                                {editable && id && (
                                                    <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                                        <Button buttonType="warning" type={'button'} onClick={deleteApplication}>
                                                            {t('common.delete')}
                                                        </Button>
                                                    </GridColumn>
                                                )}
                                            </GridRow>
                                        </form>
                                    </GridColumn>
                                </GridRow>
                            </MainContent>
                            <div ref={commentsRef}>{id && <CommentSection contentId={id} />}</div>
                        </>
                    </QueryGuard>
                </>
            )}
        </>
    );
}
