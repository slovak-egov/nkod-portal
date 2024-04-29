import { yupResolver } from '@hookform/resolvers/yup';
import { useState } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Link } from 'react-router-dom';
import { buildYup } from 'schema-to-yup';
import { ref, string } from 'yup';
import idskLogo from '../assets/images/idsk_favicon.jpg';
import { UserRegistrationForm, useUserRegister } from '../client';
import BaseInput from '../components/BaseInput';
import Breadcrumbs from '../components/Breadcrumbs';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
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
                                        <a
                                            href="#"
                                            role="button"
                                            draggable="false"
                                            className="idsk-button idsk-button--start idsk-button idsk-button--secondary govuk-!-width-full"
                                            data-module="idsk-button"
                                        >
                                            <img src={idskLogo} alt="eGovernment" height={24} />
                                            <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.eGovernment')}</span>
                                        </a>
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <a
                                            href="#"
                                            role="button"
                                            draggable="false"
                                            className="idsk-button idsk-button--start idsk-button idsk-button--secondary  govuk-!-width-full"
                                            data-module="idsk-button"
                                        >
                                            <svg xmlns="http://www.w3.org/2000/svg" height="24" viewBox="0 0 24 24" width="24">
                                                <path
                                                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                                                    fill="#4285F4"
                                                />
                                                <path
                                                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                                                    fill="#34A853"
                                                />
                                                <path
                                                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                                                    fill="#FBBC05"
                                                />
                                                <path
                                                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                                                    fill="#EA4335"
                                                />
                                                <path d="M1 1h22v22H1z" fill="none" />
                                            </svg>
                                            <span style={{ paddingLeft: '0.75rem' }}>{t('loginPage.socialLogin.google')}</span>
                                        </a>
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
