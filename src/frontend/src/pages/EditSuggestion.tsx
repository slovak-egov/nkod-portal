import React, { useContext } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';

import { CodelistValue, sendPost, TokenContext, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import { EditSuggestionFormValues, SuggestionStatusCode, SuggestionType } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import FormElementGroup from '../components/FormElementGroup';
import SelectElementItems from '../components/SelectElementItems';
import BaseInput from '../components/BaseInput';
import TextArea from '../components/TextArea';
import Button from '../components/Button';
import { suggestionTypeCodeList } from './AddSuggestion';

const EditSuggestion = () => {
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('editSuggestion.headerTitle'));
    const tokenContext = useContext(TokenContext);

    const { control, register, handleSubmit, watch, formState: { errors } } = useForm<EditSuggestionFormValues>();

    const onSubmit: SubmitHandler<EditSuggestionFormValues> = async (data) => {
        const result = await sendPost<EditSuggestionFormValues>(`/api/suggestion`, data, headers);
        // setSaveResult(result.data);
    };

    const suggestionOrganizationCodeList: CodelistValue[] = [
        {
            id: '1',
            label: 'Úrad'
        },
        {
            id: '2',
            label: 'Organizácia'
        }
    ];


    const suggestionStatusList: CodelistValue[] = [
        {
            id: SuggestionStatusCode.PROPOSAL_FOR_CHANGE,
            label: 'Návrh na zmenu'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_FOR_CREATIOM,
            label: 'Návrh na vytvorenie'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_REJECTED,
            label: 'Návrh zamietnutý'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_APPROVED,
            label: 'Návrh schválený'
        },
        {
            id: SuggestionStatusCode.PROPOSAL_IN_PROGRESS,
            label: 'Návrh v riešení'
        }
    ];

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('editSuggestion.headerTitle') }]} />
            <MainContent>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <PageHeader>{t('editSuggestion.title', {name: 'Zverejnenie CRZ cez OpenDataAPI'})}</PageHeader>

                    <p>
                        {t('editSuggestion.userTitle', { name: `${userInfo?.firstName} ${userInfo?.lastName}` })}
                    </p>
                    <p>
                        {t('editSuggestion.organizationTitle', { name: userInfo?.companyName })}
                    </p>

                    <h2 className='govuk-heading-m '>
                        {t('editSuggestion.subtitle')}
                    </h2>

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('editSuggestion.fields.organization')}
                                element={id => <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={false}
                                    options={suggestionOrganizationCodeList}
                                    selectedValue={field.value}
                                    onChange={field.onChange}
                                    renderOption={v => v.label}
                                    getValue={v => v.id}
                                />}
                            />
                        )}
                        name='organization'
                        control={control}
                    />

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('editSuggestion.fields.suggestionType')}
                                element={id => <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={false}
                                    options={suggestionTypeCodeList}
                                    selectedValue={field.value}
                                    onChange={field.onChange}
                                    renderOption={v => v.label}
                                    getValue={v => v.id}
                                />}
                            />
                        )}
                        name='suggestionType'
                        control={control}
                    />

                    <FormElementGroup
                        label={t('editSuggestion.fields.suggestionTitle')}
                        element={id =>
                            <BaseInput
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                                placeholder='Výber'
                            />
                        }
                    />

                    <FormElementGroup
                        label={t('editSuggestion.fields.suggestionDescription')}
                        element={id =>
                            <TextArea
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                            />
                        }
                    />


                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('editSuggestion.fields.suggestionStatus')}
                                element={id => <SelectElementItems<CodelistValue>
                                    id={id}
                                    disabled={false}
                                    options={suggestionStatusList}
                                    selectedValue={field.value}
                                    onChange={field.onChange}
                                    renderOption={v => v.label}
                                    getValue={v => v.id}
                                />}
                            />
                        )}
                        name='suggestionStatus'
                        control={control}
                    />

                    <Button >
                        {t('editSuggestion.addButton')}
                    </Button>
                </form>
                <a className="govuk-link" title={t('idsk')} href="https://idsk.gov.sk/">
                    {t('editSuggestion.commentButton')}
                </a>.
            </MainContent>
        </>
    );
};

export default EditSuggestion;
