import { yupResolver } from '@hookform/resolvers/yup';
import { useState } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Link } from 'react-router-dom';
import { buildYup } from 'schema-to-yup';
import { ref, string } from 'yup';
import { LoginMethod, UserRegistrationForm, useUserRegister } from '../client';
import BaseInput from '../components/BaseInput';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LoginExternalButton from '../components/LoginExternalButton';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { useSchemaConfig } from '../helpers/helpers';
import SuccessErrorPage from './SuccessErrorPage';
import { schema } from './schemas/RegistrationSchema';

export default function RegisterUser() {
    const navigate = useNavigate();
    const { t } = useTranslation();
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));
    const [saveSuccess, setSaveSuccess] = useState<boolean>(false);
    const [savingUser, errorSaving, saveUser] = useUserRegister();

    const extendedSchema = yupSchema.shape({
        passwordConfirm: string()
            .required(t('validation.required'))
            .oneOf([ref('password')], t('registerPage.fields.passwordMatchError'))
    });

    const form = useForm<UserRegistrationForm>({
        resolver: yupResolver(extendedSchema)
    });

    const {
        register,
        handleSubmit,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<UserRegistrationForm> = async (data) => {
        const result: any = await saveUser(data);
        if (result?.success) {
            setSaveSuccess(true);
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('header.registration') }]} />
            {saveSuccess || errorSaving ? (
                <SuccessErrorPage
                    isSuccess={!errorSaving}
                    msg={errorSaving?.message ?? t('registrationSuccessful')}
                    backButtonLabel={t('common.backToMain')}
                    backButtonClick={() => navigate('/')}
                />
            ) : (
                <MainContent>
                    <PageHeader>{t('registerPage.title')}</PageHeader>

                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.firstName')}
                                            errorMessage={errors.firstName?.message}
                                            element={(id) => <BaseInput id={id} disabled={savingUser} {...register('firstName')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.lastName')}
                                            errorMessage={errors.lastName?.message}
                                            element={(id) => <BaseInput id={id} disabled={savingUser} {...register('lastName')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.email')}
                                            errorMessage={errors.email?.message}
                                            element={(id) => <BaseInput id={id} type={'email'} disabled={savingUser} {...register('email')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('common.password')}
                                            errorMessage={errors.password?.message}
                                            element={(id) => <BaseInput id={id} type={'password'} disabled={savingUser} {...register('password')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.passwordConfirm')}
                                            errorMessage={errors.passwordConfirm?.message}
                                            element={(id) => <BaseInput id={id} type={'password'} disabled={savingUser} {...register('passwordConfirm')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={2}>
                                        <Button>{t('registerPage.registerButton')}</Button>
                                    </GridColumn>
                                    <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                        <Button
                                            buttonType={'secondary'}
                                            onClick={async () => {
                                                navigate('/');
                                            }}
                                        >
                                            {t('registerPage.cancelButton')}
                                        </Button>
                                    </GridColumn>
                                </GridRow>

                                <h2 className="govuk-heading-m">{t('loginPage.socialLogin.title')}</h2>

                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <LoginExternalButton method={LoginMethod.EGOV} />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <LoginExternalButton method={LoginMethod.GOOGLE} />
                                    </GridColumn>
                                </GridRow>

                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} className="govuk-!-text-align-centre">
                                        <Link to="/prihlasenie" className="idsk-card-title govuk-link">
                                            {t('registerPage.loginLink')}
                                        </Link>
                                    </GridColumn>
                                </GridRow>
                            </form>
                        </GridColumn>
                    </GridRow>
                </MainContent>
            )}
        </>
    );
}
