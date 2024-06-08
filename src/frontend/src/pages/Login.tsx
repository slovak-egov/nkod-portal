import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { Link } from 'react-router-dom';

import { yupResolver } from '@hookform/resolvers/yup';
import { useContext } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';
import { buildYup } from 'schema-to-yup';
import { LoginMethod, TokenContext, UserLoginForm, useUserLogin } from '../client';
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
import { schema } from './schemas/LoginSchema';

export default function Login() {
    const navigate = useNavigate();
    const { t } = useTranslation();
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));
    const tokenContext = useContext(TokenContext);
    const [loggingUser, errorLogin, loginUser] = useUserLogin();

    const form = useForm<UserLoginForm>({
        resolver: yupResolver(yupSchema)
    });

    const {
        register,
        handleSubmit,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<UserLoginForm> = async (data) => {
        const result: any = await loginUser(data);
        if (result?.data?.data?.token) {
            tokenContext?.setToken({ ...result?.data?.data });
            navigate('/');
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('login') }]} />
            {errorLogin ? (
                <SuccessErrorPage
                    isSuccess={!errorLogin}
                    msg={errorLogin?.message ?? t('registrationSuccessful')}
                    backButtonLabel={t('common.backToMain')}
                    backButtonClick={() => navigate('/')}
                />
            ) : (
                <MainContent>
                    <PageHeader>{t('login')}</PageHeader>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <h2 className="govuk-heading-m ">{t('loginPage.subtitle')}</h2>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.email')}
                                            errorMessage={errors.email?.message}
                                            element={(id) => <BaseInput id={id} disabled={loggingUser} {...register('email')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.password')}
                                            errorMessage={errors.password?.message}
                                            element={(id) => <BaseInput id={id} type={'password'} disabled={loggingUser} {...register('password')} />}
                                        />
                                    </GridColumn>
                                </GridRow>

                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={2}>
                                        <Button>{t('loginPage.loginButton')}</Button>
                                    </GridColumn>
                                    <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                        <Button
                                            buttonType={'secondary'}
                                            onClick={async () => {
                                                navigate('/');
                                            }}
                                        >
                                            {t('loginPage.cancelButton')}
                                        </Button>
                                    </GridColumn>
                                </GridRow>
                            </form>

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
                                <GridColumn widthUnits={1} totalUnits={2}>
                                    <Link to="/zabudnute-heslo" className="idsk-card-title govuk-link">
                                        {t('loginPage.problemLink')}
                                    </Link>
                                </GridColumn>
                                <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                    <Link to="/registracia" className="idsk-card-title govuk-link">
                                        {t('loginPage.registerLink')}
                                    </Link>
                                </GridColumn>
                            </GridRow>
                        </GridColumn>
                    </GridRow>
                </MainContent>
            )}
        </>
    );
}
