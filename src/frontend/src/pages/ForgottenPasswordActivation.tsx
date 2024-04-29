import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';

import { yupResolver } from '@hookform/resolvers/yup';
import { SubmitHandler, useForm } from 'react-hook-form';
import { buildYup } from 'schema-to-yup';
import { UserForgottenPasswordActivationForm, useUserForgottenActivationPassword } from '../client';
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
import { schema } from './schemas/ForgottenPasswordActivationSchema';
import { ref, string } from 'yup';
import { useSearchParams } from 'react-router-dom';

export default function ForgottenPasswordActivation() {
    const navigate = useNavigate();
    const { t } = useTranslation();
    const [searchParams] = useSearchParams();
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));
    const [success, sending, error, changePassword] = useUserForgottenActivationPassword();

    const extendedSchema = yupSchema.shape({
        passwordConfirm: string()
            .required(t('validation.required'))
            .oneOf([ref('password')], t('registerPage.fields.passwordMatchError'))
    });

    const form = useForm<UserForgottenPasswordActivationForm>({
        resolver: yupResolver(extendedSchema),
        defaultValues: {
            id: searchParams.get('id'),
            token: searchParams.get('token')
        }
    });

    const {
        register,
        handleSubmit,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<UserForgottenPasswordActivationForm> = async (data) => {
        await changePassword(data);
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('forgottenPasswordPage.activation.newPassword') }]} />
            {success || error ? (
                <SuccessErrorPage
                    isSuccess={success}
                    msg={error?.message ?? t('forgottenPasswordActivationSuccessful')}
                    backButtonLabel={t('common.backToMain')}
                    backButtonClick={() => navigate('/')}
                />
            ) : (
                <MainContent>
                    <PageHeader>{t('forgottenPasswordPage.activation.newPassword')}</PageHeader>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('common.password')}
                                            errorMessage={errors.password?.message}
                                            element={(id) => <BaseInput id={id} type={'password'} disabled={sending} {...register('password')} />}
                                        />
                                    </GridColumn>
                                </GridRow>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.passwordConfirm')}
                                            errorMessage={errors.passwordConfirm?.message}
                                            element={(id) => <BaseInput id={id} type={'password'} disabled={sending} {...register('passwordConfirm')} />}
                                        />
                                    </GridColumn>
                                </GridRow>

                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={2}>
                                        <Button>{t('forgottenPasswordPage.activation.newPassword')}</Button>
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
                        </GridColumn>
                    </GridRow>
                </MainContent>
            )}
        </>
    );
}
