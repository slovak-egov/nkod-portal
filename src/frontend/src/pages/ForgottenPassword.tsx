import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';

import { yupResolver } from '@hookform/resolvers/yup';
import { SubmitHandler, useForm } from 'react-hook-form';
import { buildYup } from 'schema-to-yup';
import { UserForgottenPasswordForm, useUserForgottenPassword } from '../client';
import Alert from '../components/Alert';
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
import { schema } from './schemas/ForgottenPasswordSchema';

export default function ForgottenPassword() {
    const navigate = useNavigate();

    const { t } = useTranslation();

    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));
    const [success, sending, errorsPassword, sendEmail] = useUserForgottenPassword();

    const form = useForm<UserForgottenPasswordForm>({
        resolver: yupResolver(yupSchema)
    });

    const {
        register,
        handleSubmit,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<UserForgottenPasswordForm> = async (data) => {
        await sendEmail(data);
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('forgottenPasswordPage.title') }]} />
            {success ? (
                <SuccessErrorPage msg={t('forgottenPasswordSuccessful')} backButtonLabel={t('common.backToMain')} backButtonClick={() => navigate('/')} />
            ) : (
                <MainContent>
                    <PageHeader>{t('forgottenPasswordPage.title')}</PageHeader>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1}>
                                        <FormElementGroup
                                            label={t('registerPage.fields.email')}
                                            errorMessage={errors.email?.message}
                                            element={(id) => <BaseInput id={id} disabled={sending} {...register('email')} />}
                                        />
                                    </GridColumn>
                                </GridRow>

                                {errorsPassword && errorsPassword?.length > 0 && (
                                    <Alert type="warning">
                                        {errorsPassword?.map((err, idx) => (
                                            <p className="govuk-!-padding-left-3" key={idx}>
                                                {err.message}
                                            </p>
                                        ))}
                                    </Alert>
                                )}

                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={2}>
                                        <Button>{t('forgottenPasswordPage.recovery')}</Button>
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
