import React, { useContext } from 'react';
import { useTranslation } from 'react-i18next';
import { Controller, SubmitHandler, useForm } from 'react-hook-form';

import { CodelistValue, sendPost, TokenContext, useDefaultHeaders, useDocumentTitle, useUserInfo } from '../client';
import { SuggestionFormValues, SuggestionType, useCmsPublisherLists } from '../cms';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import FormElementGroup from '../components/FormElementGroup';
import SelectElementItems from '../components/SelectElementItems';
import BaseInput from '../components/BaseInput';
import TextArea from '../components/TextArea';
import Button from '../components/Button';
import Select from 'react-select';

export const suggestionTypeCodeList: CodelistValue[] = [
    {
        id: SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET,
        label: 'podnet na zverejnenie nového datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET,
        label: 'podnet na kvalitu dát zverejneného datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_METADATA,
        label: 'podnet na kvalitu metadát zverejneného datasetu/API'
    },
    {
        id: SuggestionType.SUGGESTION_OTHER,
        label: 'iný podnet'
    }
];

const AddSuggestion = () => {
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { t } = useTranslation();
    useDocumentTitle(t('addApplicationPage.headerTitle'));
    const tokenContext = useContext(TokenContext);

    const [organizationCodeList, loadingOrganizationCodeList, errorOrganizationCodeList] = useCmsPublisherLists({ language: 'sk' });

    const { control, register, handleSubmit, watch, formState: { errors } } = useForm<SuggestionFormValues>();

    const onSubmit: SubmitHandler<SuggestionFormValues> = async (data) => {
        const result = await sendPost<SuggestionFormValues>(`/api/suggestion`, data, headers);
        // setSaveResult(result.data);
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('addSuggestion.headerTitle') }]} />
            <MainContent>
                <form onSubmit={handleSubmit(onSubmit)}>
                    <PageHeader>{t('addSuggestion.title')}</PageHeader>

                    <p>
                        {t('addSuggestion.userTitle', { name: `${userInfo?.firstName} ${userInfo?.lastName}` })}
                    </p>
                    <p>
                        {t('addSuggestion.organizationTitle', { name: userInfo?.companyName })}
                    </p>

                    <h2 className='govuk-heading-m '>
                        {t('addSuggestion.subtitle')}
                    </h2>

                    {/*<Controller*/}
                    {/*    render={({ field }) => (*/}
                    {/*        <FormElementGroup*/}
                    {/*            label={t('addSuggestion.fields.organization')}*/}
                    {/*            element={id => <SelectElementItems<CodelistValue>*/}
                    {/*                id={id}*/}
                    {/*                disabled={false}*/}
                    {/*                options={organizationCodeList}*/}
                    {/*                selectedValue={field.value}*/}
                    {/*                onChange={field.onChange}*/}
                    {/*                renderOption={v => v.label}*/}
                    {/*                getValue={v => v.id}*/}
                    {/*            />}*/}
                    {/*        />*/}
                    {/*    )}*/}
                    {/*    name='organization'*/}
                    {/*    control={control}*/}
                    {/*/>*/}

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('addSuggestion.fields.organization')}
                                element={id => <Select
                                    // className='govuk-select'
                                    // classNamePrefix='govuk-select'
                                    classNames={{

                                    }}
                                    options={organizationCodeList}
                                    value={organizationCodeList.find(x => x.value === field.value)}
                                    getOptionLabel={x => x.label}
                                    onChange={x => field.onChange(x?.value)}
                                />}
                            />
                        )}
                        name='organization'
                        control={control}
                    />

                    <Controller
                        render={({ field }) => (
                            <FormElementGroup
                                label={t('addSuggestion.fields.suggestionType')}
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
                        label={t('addSuggestion.fields.suggestionTitle')}
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
                        label={t('addSuggestion.fields.suggestionDescription')}
                        element={id =>
                            <TextArea
                                id={id}
                                disabled={false}
                                {...register('suggestionTitle')}
                            />
                        }
                    />

                    <Button>
                        {t('addSuggestion.addButton')}
                    </Button>

                </form>
            </MainContent>
        </>
    );
};

export default AddSuggestion;
